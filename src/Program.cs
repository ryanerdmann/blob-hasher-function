using Azure.Core;
using Azure.Identity;
using Azure.Storage;
using BlobHasher.Function.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        if (context.HostingEnvironment.IsDevelopment())
        {
            // Azurite Connection String
            services.AddSingleton(new StorageSharedKeyCredential(
                "devstoreaccount1",
                "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw=="));
        }
        else
        {
            services.AddSingleton<TokenCredential>(new DefaultAzureCredential());
        }

        services.AddSingleton<BlobHasherService>();
        services.AddSingleton<BlobClientFactory>();

        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
    })
    .Build();

host.Run();
