# Blob Hasher - Azure Function

This Azure Function listens for `BlobCreatedEvent` event notifications from an Azure Storage account.
Whenever a new blob is uploaded, the function downloads the blob, computes its hash, and stores the hash back on the blob.

Hashes are stored both in the `Content-MD5` property and as custom metadata on the blob.

This service currently supports three checksum types:
1. MD5
2. SHA256
3. CRC64C (CRC64NVME)

> [!WARNING]  
> This code is for demonstration purposes only. It is **not** production-ready, and no warranty is implied by the author.

## Deployment

Below are the high-level steps to deploy and configure the function. Depending on your environment, you may need to adjust these steps accordingly.

1. **Download the Latest Release**  
   - Visit the [GitHub Releases](https://github.com/ryanerdmann/blob-hasher-function/releases) page and download the latest release package of this function.

2. **Create and Deploy the Azure Function**  
   - [Create a new Azure Function App](https://learn.microsoft.com/azure/azure-functions/functions-create-function-app-portal) in the Azure Portal or via the Azure CLI.  
   - Configure [Application Insights](https://learn.microsoft.com/azure/azure-functions/functions-monitoring?tabs=portal) for monitoring and logging.  
   - Deploy the function code (the `.zip` you downloaded) to your Function App. This can be done using [Azure CLI](https://learn.microsoft.com/en-us/azure/azure-functions/deployment-zip-push), Visual Studio Code, or other deployment tools.

3. **Assign a Managed Identity and Grant Required Permissions**  
   - In your Function App’s **Identity** settings, enable a system-assigned managed identity (or user-assigned if you prefer).  
   - Go to your target Storage Account’s **Access Control (IAM)** and add a **role assignment** for the Function App’s managed identity. Make sure to assign the **Storage Blob Data Contributor** role so the function can read and write blob data.

4. **Create an Event Grid System Topic**  
   - In your Azure Subscription, [create an Event Grid System Topic](https://learn.microsoft.com/en-us/azure/event-grid/create-view-manage-system-topics) for your Storage Account.  
   - Ensure the Event Grid can reach the function endpoint (the Azure Function App should be publicly accessible or using a private endpoint appropriately configured).

5. **Enable Event Notifications on the Storage Account**  
   - In the Azure Portal for your Storage Account, select **Events**.  
   - Under "More Options," select Event Grid Namespace Topic.
   - Under "Topic Details," ensure the topic created in Step 4 is selected as the destination.
   - Under "Event Types," select `Blob Created`.
   - Under "Endpoint Details," select the Azure Function instance.
   - Under "Additional Features," consider enabling dead lettering and retry policies.

6. **Test the Function**  
   - Upload a blob to the monitored storage container.  
   - Confirm that the Azure Function is triggered and that it successfully computes and stores the blob’s hash as metadata. You can verify this using Azure Storage Explorer or the Azure Portal to inspect the blob’s metadata.

## Contributing & License

- Contributions, bug reports, and feature requests are welcome! Open a PR or create an issue.
- This project is licensed under the terms of the MIT license (or whichever license you choose).
