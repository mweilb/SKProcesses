 
using Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using OllamaSharp;
 
 
#pragma warning restore SKEXP0080
public static class KernelSetup
{
    public static int EmbeddingDimension { get; set; }

    public static void SetupAzure(IKernelBuilder kernelBuilder, IConfiguration configuration)
    {
        EmbeddingDimension = 1536;
        var apiKey = configuration["AzureOpenAI:ApiKey"];
        var endpoint = configuration["AzureOpenAI:Endpoint"];
        var deploymentName = configuration["AzureOpenAI:Deployment"];
        var azureSearchEndpoint = configuration["AzureOpenAI:SearchEndpoint"];
        var azureSearchKey = configuration["AzureOpenAI:SearchKey"];

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(deploymentName))
        {
            throw new InvalidOperationException("Azure OpenAI configuration values are not set.");
        }

        var azureAiChatService = new AzureOpenAIChatCompletionService(
            deploymentName: deploymentName,
            endpoint: endpoint,
            apiKey: apiKey
        );
 
        kernelBuilder.Services.AddSingleton<IChatCompletionService>(azureAiChatService);

        if (!(string.IsNullOrEmpty(azureSearchEndpoint) || string.IsNullOrEmpty(azureSearchKey)))
        {
#pragma warning disable SKEXP0010
            kernelBuilder.AddAzureOpenAITextEmbeddingGeneration(
                deploymentName: deploymentName,
                endpoint: endpoint,
                apiKey: apiKey
            );
#pragma warning restore SKEXP0010

            kernelBuilder.AddAzureAISearchVectorStore(
                new Uri(azureSearchEndpoint),
                new AzureKeyCredential(azureSearchKey)
            );
        }
    }

    public static Kernel SetupKernel(IConfiguration configuration, string setupForLlmRequested)
    {
        var kernelBuilder = Kernel.CreateBuilder();

        // Configure LLM provider
        if (setupForLlmRequested == "Ollama")
        {
            SetupOllama(kernelBuilder, configuration);
        }
        else
        {
            SetupAzure(kernelBuilder, configuration);
        }

         
        // Add logging to the kernel
        kernelBuilder.Services.AddLogging();

        // Create a base logger factory with a built‐in provider (like Console).
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();
        });

        kernelBuilder.Services.AddSingleton(loggerFactory);

        var kernel = kernelBuilder.Build();

        return kernel;
    }
    public static void SetupOllama(IKernelBuilder kernelBuilder, IConfiguration configuration)
    {
        EmbeddingDimension = 3584;
        var ollamaEndpoint = configuration["OLLAMA_ENDPOINT"] ?? "http://localhost:11434";
        var modelId = configuration["OLLAMA_MODEL"] ?? "deepseek-r1";
        var ollamaUri = new Uri(ollamaEndpoint);

#pragma warning disable SKEXP0070
        var ollamaClient = new OllamaApiClient(uriString: ollamaEndpoint, defaultModel: modelId);
        kernelBuilder.Services.AddSingleton(ollamaClient);
#pragma warning disable SKEXP0001
        var chatService = ollamaClient.AsChatCompletionService();
#pragma warning restore SKEXP0001
 
        kernelBuilder.Services.AddSingleton<IChatCompletionService>(chatService);
        kernelBuilder.AddOllamaTextEmbeddingGeneration(modelId, ollamaUri);
#pragma warning restore SKEXP0070
    }
 
}
