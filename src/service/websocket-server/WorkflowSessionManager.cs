using System.Text.Json;
using AgenticWorkflowSK;
using AgenticWorkflowSKSample;
using AgenticWorkflowSKSample.Workflow1;
using Microsoft.SemanticKernel;
 
public class WorkflowSessionManager
{
    private WorkflowProcess<PropertyBag>? _workflow;
    private PropertyBag? _state;
    private IAsyncEnumerator<WorkflowProcess<PropertyBag>.ProcessIterationResult>? _workflowEnumerator;
    private readonly LlmProviderFactory _llmProviderFactory;
    private readonly PromptsConfig _promptsConfig;
    private readonly WebSocketMessageSender _messageSender;

    public PropertyBag? State => _state;
    public WorkflowProcess<PropertyBag>? Workflow => _workflow;
    public IAsyncEnumerator<WorkflowProcess<PropertyBag>.ProcessIterationResult>? Enumerator => _workflowEnumerator;

    public WorkflowSessionManager(LlmProviderFactory llmProviderFactory, PromptsConfig promptsConfig, WebSocketMessageSender messageSender)
    {
        _llmProviderFactory = llmProviderFactory;
        _promptsConfig = promptsConfig;
        _messageSender = messageSender;
    }

    public async Task LoadWorkflowAsync(JsonElement root)
    {
        var yaml = root.GetProperty("yaml").GetString() ?? "";
#pragma warning disable SKEXP0080
        var (builder, externalEdges) = Workflow1.BuildSteps(_promptsConfig);
        var steps = builder.Build();
#pragma warning restore SKEXP0080

        var kernel = _llmProviderFactory.CreateKernel();
        _workflow = new WorkflowProcess<PropertyBag>(kernel, steps, externalEdges);

        string keyword = root.TryGetProperty("keyword", out var keywordElem) && keywordElem.ValueKind == JsonValueKind.String
            ? keywordElem.GetString() ?? "stock options trading in non-normal times"
            : "stock options trading in non-normal times";

        _state = new PropertyBag
        {
            ["KeyWord"] = keyword,
            ["History"] = new List<string>()
        };

        if (_workflowEnumerator != null)
        {
            await _workflowEnumerator.DisposeAsync();
            _workflowEnumerator = null;
        }

        if (_workflow != null)
        {
            _workflow.MessageChannel.RegisterExternalEventListener(OnExternalEventEmittedHandler);
        }

        if (_workflow != null && _state != null)
        {
            _workflowEnumerator = _workflow.IterateAsync(_state).GetAsyncEnumerator();
        }
    }

    #pragma warning disable SKEXP0080
    private void OnExternalEventEmittedHandler(string externalTopicEvent, KernelProcessProxyMessage message, bool isTraceEvent)
    {
        if (isTraceEvent)
        {
            var (from, _to, id) = WorkflowTraceEvent.ParseFromToId(externalTopicEvent) ?? ("", "", "");
            _ = _messageSender.SendActiveStateAsync(id, from);
        }

        Console.WriteLine($"[WorkflowSessionManager] External event emitted: {externalTopicEvent}, Trace: {isTraceEvent}");
    }

    public async Task<bool> AdvanceWorkflowAsync()
    {
        if (_workflowEnumerator == null)
            return false;

        if (await _workflowEnumerator.MoveNextAsync())
        {
            var evt = _workflowEnumerator.Current;
            if (evt == null || evt.Data == null)
                return false;

            _state = evt.Data;
            return true;
        }
        else
        {
            await _workflowEnumerator.DisposeAsync();
            _workflowEnumerator = null;
            return false;
        }
    }

    public void SetNextIteraction(string eventId)
    {
        _workflow?.SetNextIteraction(eventId);
    }
}
