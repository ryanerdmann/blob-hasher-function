using System.Diagnostics;
using Azure.Messaging.EventGrid.SystemEvents;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BlobHasher.Function.Services;

public class BlobHasherService
{
    private readonly ILogger<BlobHasherService> _logger;
    private readonly BlobClientFactory _factory;
    private readonly IHostEnvironment _environment;

    public BlobHasherService(ILogger<BlobHasherService> logger, BlobClientFactory factory, IHostEnvironment environment)
    {
        _logger = logger;
        _factory = factory;
        _environment = environment;
    }

    public async Task HashBlobAsync(StorageBlobCreatedEventData eventData, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Hashing blob '{Uri}' ETag={ETag} Length={ContentLength} BlobType={BlobType}",
            eventData.Url,
            eventData.ETag,
            eventData.ContentLength,
            eventData.BlobType);


        //
        // Create a BlobClient, and use an ETag condition to ensure
        // the blob hasn't changed since the event was received.
        //

        var blob = _factory.CreateBlobClient(eventData.Url);

        var condition = new BlobRequestConditions
        {
            IfMatch = new Azure.ETag(eventData.ETag),
        };

        if (_environment.IsDevelopment())
        {
            // Since Azurite does not support stable ETags, we must fetch
            // the latest ETag for testing purposes.
            var TEMP_ETAG = blob.GetProperties().Value.ETag;
            condition.IfMatch = TEMP_ETAG;
        }


        //
        // Download the blob and compute the hash.
        //

        var checksums = await blob.GetChecksumsAsync(BlobChecksumType.All, condition, cancellationToken);


        //
        // Set the standard Content-MD5 header for validation.
        // Also set the checksums as custom metadata on the blob.
        //

        if (checksums.MD5 is null)
        {
            throw new InvalidOperationException("MD5 checksum is required.");
        }

        var headers = new BlobHttpHeaders
        {
            ContentHash = checksums.MD5,
        };

        var metadata = new Dictionary<string, string>();
        foreach (var checksum in checksums.GetAllChecksums())
        {
            metadata[checksum.Name] = checksum.CanonicalStringRepresentation;
        }

        _logger.LogInformation("Computed checksums for blob '{Uri}': {Checksums}",
            eventData.Url,
            string.Join("; ", metadata.Select(m => $"{m.Key}={m.Value}")));

        await blob.SetPropertiesAsync(headers, metadata, condition, cancellationToken);
    }
}
