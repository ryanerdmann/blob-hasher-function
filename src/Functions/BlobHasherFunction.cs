using System.Diagnostics;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using BlobHasher.Function.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace BlobHasher.Function.Functions;

public class BlobHasherFunction
{
    private readonly ILogger<BlobHasherFunction> _logger;
    private readonly BlobHasherService _hasher;

    public BlobHasherFunction(ILogger<BlobHasherFunction> logger, BlobHasherService hasher)
    {
        _logger = logger;
        _hasher = hasher;
    }

    [Function(nameof(BlobHasherFunction))]
    public async Task Run([EventGridTrigger] CloudEvent cloudEvent)
    {
        if (!cloudEvent.TryGetSystemEventData(out object eventData))
        {
            _logger.LogWarning("Event is not a known EventGrid system event. Type={Type}", cloudEvent.Type);
            return;
        }

        if (eventData is not StorageBlobCreatedEventData blobCreatedEvent)
        {
            _logger.LogWarning("Event is not a StorageBlobCreated event. Type={Type}", cloudEvent.Type);
            return;
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _hasher.HashBlobAsync(blobCreatedEvent, CancellationToken.None);

            _logger.LogInformation(
                "Succesfully hashed blob. Blob='{Uri}' Length={Length} BlobType={BlobType} Elapsed={Elapsed}ms",
                blobCreatedEvent.Url,
                blobCreatedEvent.ContentLength,
                blobCreatedEvent.BlobType,
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                "Failed to hash blob. Blob='{Uri}' Length={Length} BlobType={BlobType} Elapsed={Elapsed}ms",
                blobCreatedEvent.Url,
                blobCreatedEvent.ContentLength,
                blobCreatedEvent.BlobType,
                stopwatch.ElapsedMilliseconds);

            // Rethrow the exception so the function fails and we get a retry.
            throw;
        }
    }
}
