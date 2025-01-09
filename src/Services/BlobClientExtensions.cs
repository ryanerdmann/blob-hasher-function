using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace BlobHasher.Function.Services;

internal static class BlobClientExtensions
{
    public static async Task<BlobChecksumCollection> GetChecksumsAsync(
        this BlobClient blob,
        BlobChecksumType checksums,
        BlobRequestConditions condition,
        CancellationToken cancellationToken)
    {
        var options = new BlobDownloadToOptions
        {
            Conditions = condition,
        };

        using var stream = BlobHashingStream.Create(checksums);

        await blob.DownloadToAsync(stream, options, cancellationToken);

        return stream.GetChecksums();
    }

    public static async Task SetPropertiesAsync(
        this BlobClient blob,
        BlobHttpHeaders headers,
        IDictionary<string, string> metadata,
        BlobRequestConditions condition,
        CancellationToken cancellationToken)
    {
        // Since the SDK does not provided a combined SetProperties API,
        // we must call both SetHttpHeaders and SetMetadata separately,
        // with a causal ETag check.

        var setHeadersResponse = await blob.SetHttpHeadersAsync(headers, condition, cancellationToken);

        var setMetadataCondition = new BlobRequestConditions
        {
            LeaseId = condition.LeaseId,
            IfMatch = setHeadersResponse.Value.ETag,
        };

        await blob.SetMetadataAsync(metadata, setMetadataCondition, cancellationToken);
    }
}
