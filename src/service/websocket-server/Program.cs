using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using AgenticWorkflowSK;
using AgenticWorkflowSKSample;
using AgenticWorkflowSKSample.Workflow1;
using Microsoft.SemanticKernel;
using AgenticWorkflowSKSample.Workflow1.Steps;

#pragma warning disable SKEXP0080

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Load configuration (appsettings.json, localsettings.json, env vars)
var configBuilder = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile("localsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();
IConfiguration configuration = configBuilder.Build();

// Determine configuration.yml path
string? configLocation = configuration.GetValue<string>("ConfigurationLocation");
string configYmlPath = !string.IsNullOrEmpty(configLocation)
    ? Path.Combine(configLocation, "Contexts\\configuration.yml")
    : "../shared/AgenticWorkflowSKSample/Contexts/configuration.yml";

// Load prompts config
var promptsConfig = Workflow1.LoadConfigurations(configYmlPath);

app.UseWebSockets();

var llmProviderFactory = new LlmProviderFactory();

app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var handler = new WorkflowWebSocketHandler(webSocket, llmProviderFactory, promptsConfig);
        await handler.HandleAsync();
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

app.Run();
