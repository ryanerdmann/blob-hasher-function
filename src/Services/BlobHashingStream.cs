using System.IO.Hashing;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Azure.Storage;

namespace BlobHasher.Function.Services;

[Flags]
internal enum BlobChecksumType
{
    None = 1 << 0,
    MD5 = 1 << 1,
    SHA256 = 1 << 2,
    CRC64NVME = 1 << 3,

    All = MD5 | SHA256 | CRC64NVME,
}

internal struct BlobChecksumCollection
{
    public byte[]? MD5 { get; init; }
    public byte[]? SHA256 { get; init; }
    public ulong? CRC64NVME { get; init; }

    public IEnumerable<BlobChecksumValue> GetAllChecksums()
    {
        if (MD5 is not null)
        {
            yield return new BlobChecksumValue
            {
                Name = "MD5",
                CanonicalStringRepresentation = Convert.ToBase64String(MD5),
            };
        }

        if (SHA256 is not null)
        {
            yield return new BlobChecksumValue
            {
                Name = "SHA256",
                CanonicalStringRepresentation = Convert.ToHexString(SHA256).ToLower(),
            };
        }

        if (CRC64NVME is not null)
        {
            yield return new BlobChecksumValue
            {
                Name = "CRC64NVME",
                CanonicalStringRepresentation = $"0x{CRC64NVME:x}",
            };
        }
    }
}

internal struct BlobChecksumValue
{
    public string Name { get; init; }
    public string CanonicalStringRepresentation { get; init; }
}

internal class BlobHashingStream : Stream
{
    private long _length;

    private MD5? _md5;
    private SHA256? _sha256;
    private NonCryptographicHashAlgorithm? _crc64NVME;

    public static BlobHashingStream Create(BlobChecksumType checksums)
    {
        return new BlobHashingStream(checksums);
    }

    private BlobHashingStream(BlobChecksumType checksums)
    {
        _length = 0;

        if (checksums.HasFlag(BlobChecksumType.MD5))
        {
            _md5 = MD5.Create();
        }

        if (checksums.HasFlag(BlobChecksumType.SHA256))
        {
            _sha256 = SHA256.Create();
        }

        if (checksums.HasFlag(BlobChecksumType.CRC64NVME))
        {
            _crc64NVME = StorageCrc64HashAlgorithm.Create();
        }
    }

    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

    public override long Length => _length;

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
        throw new NotImplementedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        UpdateHash(buffer, offset, count);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        UpdateHash(buffer, offset, count);
        return Task.CompletedTask;
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> segment))
        {
            UpdateHash(segment.Array!, segment.Offset, segment.Count);
        }
        else
        {
            var array = buffer.ToArray();
            UpdateHash(array, 0, array.Length);
        }

        return ValueTask.CompletedTask;
    }

    private void UpdateHash(byte[] buffer, int offset, int count)
    {
        _length += buffer.Length;

        if (_md5 is not null)
        {
            _md5.TransformBlock(buffer, offset, count, null, 0);
        }

        if (_sha256 is not null)
        {
            _sha256.TransformBlock(buffer, offset, count, null, 0);
        }

        if (_crc64NVME is not null)
        {
            _crc64NVME.Append(buffer.AsSpan(offset, count));
        }
    }

    public BlobChecksumCollection GetChecksums()
    {
        byte[]? md5 = null;
        if (_md5 is not null)
        {
            _md5.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            md5 = _md5.Hash;
        }

        byte[]? sha256 = null;
        if (_sha256 is not null)
        {
            _sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            sha256 = _sha256.Hash;
        }

        ulong? crc64NVME = null;
        if (_crc64NVME is not null)
        {
            Span<byte> bytes = stackalloc byte[8];
            int bytesWritten = _crc64NVME.GetCurrentHash(bytes);

            if (bytesWritten != bytes.Length)
            {
                throw new InvalidOperationException("CRC64 hash was not written correctly.");
            }

            crc64NVME = BitConverter.ToUInt64(bytes);
        }

        return new BlobChecksumCollection
        {
            MD5 = md5,
            SHA256 = sha256,
            CRC64NVME = crc64NVME,
        };
    }
}
