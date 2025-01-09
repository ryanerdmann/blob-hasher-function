using Azure.Core;
using Azure.Storage;
using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;

namespace BlobHasher.Function.Services;

public class BlobClientFactory
{
    private readonly StorageSharedKeyCredential? _keyCredential;
    private readonly TokenCredential? _tokenCredential;

    public BlobClientFactory(IServiceProvider services)
    {
        _keyCredential = services.GetService<StorageSharedKeyCredential>();
        _tokenCredential = services.GetService<TokenCredential>();
    }

    public BlobClient CreateBlobClient(string url)
    {
        if (_keyCredential is not null)
        {
            return new BlobClient(new Uri(url), _keyCredential);
        }

        if (_tokenCredential is not null)
        {
            return new BlobClient(new Uri(url), _tokenCredential);
        }

        throw new InvalidOperationException("No credentials were provided.");
    }
}
