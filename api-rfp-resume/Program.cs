using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Reflection;
using api_rfp_resume.Utils;

string _azure_OpenaiDeploymentName = Helper.GetEnvironmentVariable("AZURE_OPEN_API_DEPLOYMENT_NAME");
string _azure_OpenaiEndpoint = Helper.GetEnvironmentVariable("AZURE_OPEN_API_ENDPOINT");
string _azure_OpenaiKey = Helper.GetEnvironmentVariable("AZURE_OPEN_API_KEY");

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services => {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        services.AddTransient<Kernel>(s =>
        {
            var builder = Kernel.CreateBuilder();
            builder.AddAzureOpenAIChatCompletion(
                _azure_OpenaiDeploymentName,
                _azure_OpenaiEndpoint,
                _azure_OpenaiKey
                );
            // Not using below, but leaving in for example purposes should AI Search be used in future    
            // builder.Services.AddSingleton<SearchIndexClient>(s =>
            // {
            //     string endpoint = _apiAISearchEndpoint;
            //     string apiKey = _apiAISearchKey;
            //     return new SearchIndexClient(new Uri(endpoint), new Azure.AzureKeyCredential(apiKey));
            // });
            // The AzureAISearchService is not used in this example, but leaving in for future reference 
            // Add Singleton for AzureAISearch 
            // builder.Services.AddSingleton<IAzureAISearchService, AzureAISearchService>();

            return builder.Build();
        });
    })
    .Build();

host.Run();
