using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;

public class LlmProviderFactory
{
    private readonly IConfiguration _configuration;
    private readonly string _setupForLlmRequested;

    public LlmProviderFactory()
    {
        var configBuilder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("localsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();
        _configuration = configBuilder.Build();
        _setupForLlmRequested = _configuration.GetValue("LlmSetup", "Azure");
    }

    public Kernel CreateKernel()
    {
        return KernelSetup.SetupKernel(_configuration, _setupForLlmRequested);
    }
}
