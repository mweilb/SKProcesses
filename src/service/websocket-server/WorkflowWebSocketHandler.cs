using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using AgenticWorkflowSK;
using AgenticWorkflowSKSample;
using AgenticWorkflowSKSample.Workflow1;
using Microsoft.SemanticKernel;
using AgenticWorkflowSKSample.Workflow1.Steps;



public class WorkflowWebSocketHandler
{
    private readonly WebSocket _webSocket;
    private readonly WebSocketMessageSender _messageSender;
    private readonly WorkflowSessionManager _sessionManager;

    public WorkflowWebSocketHandler(WebSocket webSocket, LlmProviderFactory llmProviderFactory, PromptsConfig promptsConfig)
    {
        _webSocket = webSocket;
        _messageSender = new WebSocketMessageSender(webSocket);
        _sessionManager = new WorkflowSessionManager(llmProviderFactory, promptsConfig, _messageSender);
    }

    public async Task HandleAsync()
    {
        var buffer = new byte[4096];
        try
        {
            while (_webSocket.State == WebSocketState.Open)
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
                else
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    try
                    {
                        using var doc = JsonDocument.Parse(message);
                        var root = doc.RootElement;

                        if (root.TryGetProperty("action", out var actionElem))
                        {
                            var action = actionElem.GetString();
                            switch (action)
                            {
                                case "load_workflow":
                                    await HandleLoadWorkflowAsync(root);
                                    break;
                                case "advance":
                                    await HandleAdvanceAsync();
                                    break;
                                case "select_suggestion":
                                    await HandleSelectSuggestionAsync(root);
                                    break;
                                case "choose_ai":
                                    await HandleChooseAiAsync(root);
                                    break;
                                case "get_mermaid":
                                    await HandleGetMermaidAsync();
                                    break;
                                default:
                                    await _messageSender.SendStateAsync(_sessionManager.State, "Unknown action");
                                    break;
                            }
                        }
                        else
                        {
                            await _messageSender.SendStateAsync(_sessionManager.State, "No action specified");
                        }
                    }
                    catch (Exception ex)
                    {
                        await _messageSender.SendStateAsync(_sessionManager.State, $"Error: {ex.Message}");
                    }
                }
            }
        }
        catch (WebSocketException)
        {
            // Client disconnected abruptly, ignore to prevent unhandled exception
        }
        catch (Exception)
        {
            // Optionally log or handle other exceptions
        }
    }

    private async Task HandleLoadWorkflowAsync(JsonElement root)
    {
        await _sessionManager.LoadWorkflowAsync(root);

    }

 
 
    private async Task HandleAdvanceAsync()
    {
        
        var (from, _to, id) = WorkflowTraceEvent.ParseFromToId(Workflow1.TraceComputeStepInputEvent) ?? ("", "", "");
        _ = _messageSender.SendActiveStateAsync(id, from);
     
        await AdvanceWorkflowAndDispatchAsync();
    }

    private async Task HandleRequestSystemToDoWorkAsync()
    {

        _ = _messageSender.SendActiveStateAsync("ProxyStepId", "x");
        _ = _messageSender.SendActiveStateAsync("AppId", "x");

        var (from, _to, id) = WorkflowTraceEvent.ParseFromToId(Workflow1.TraceComputeStepInputEvent) ?? ("", "", "");
        _ = _messageSender.SendActiveStateAsync(id, from);

        var state = _sessionManager.State;
        var workflow = _sessionManager.Workflow;
        if (state != null && workflow != null)
        {
            if (state.TryGetValue<AIChoices?>("Suggestions", out var suggestions) && suggestions != null)
            {
                if (state.TryGetValue<List<string>>("History", out var history) && history != null)
                {
                    history.Add($"{suggestions.Options[suggestions.SelectedIndex]}");
                    state.Update("History", history);
                }
                workflow.SetNextIteraction(WorkflowProcess<PropertyBag>.StartEvent);

            }
        }
        await AdvanceWorkflowAndDispatchAsync();
    }

    private async Task AdvanceWorkflowAndDispatchAsync()
    {
        var enumerator = _sessionManager.Enumerator;
        var state = _sessionManager.State;
      

        if (enumerator == null)
        {
            await _messageSender.SendStateAsync(state, "Workflow not loaded");
            return;
        }

        if (await _sessionManager.AdvanceWorkflowAsync())
        {
            var evt = enumerator.Current;
            if (evt == null || evt.Data == null)
            {
                await _messageSender.SendStateAsync(state, "No state available");
                return;
            }

            if (evt.EventId == Workflow1.RequestSystemToDoWork)
            {
                await HandleRequestSystemToDoWorkAsync();
            }
            else if (evt.EventId == Workflow1.WaitingOnHumanIterate)
            {
                await _messageSender.SendStateAsync(_sessionManager.State, $"Event: {evt.EventId}", evt.EventId);
            }
        }
        else
        {
            await _messageSender.SendStateAsync(state, "Workflow complete");
        }
    }

    private async Task HandleSelectSuggestionAsync(JsonElement root)
    {
        _ = _messageSender.SendActiveStateAsync("ProxyStepId", "x");
        _ = _messageSender.SendActiveStateAsync("AppId", "x");
        var (from, _to, id) = WorkflowTraceEvent.ParseFromToId(Workflow1.TraceInputEventAppToDoWorkStep) ?? ("", "", "");
        _ = _messageSender.SendActiveStateAsync(id, from);

        var enumerator = _sessionManager.Enumerator;
        var state = _sessionManager.State;
        var workflow = _sessionManager.Workflow;

        if (enumerator != null && state != null)
        {
            int selectedIndex = root.GetProperty("selectedIndex").GetInt32();
            if (state.TryGetValue<AIChoices?>("Suggestions", out var suggestions) && suggestions is not null)
            {
                var updatedSuggestions = suggestions;
                updatedSuggestions.SelectedIndex = selectedIndex;
                state.Update("Suggestions", updatedSuggestions);

                workflow?.SetNextIteraction(AskAppToDoWorkStep.RequestActivity);

            }
            await AdvanceWorkflowAndDispatchAsync();
        }
        else
        {
            await _messageSender.SendStateAsync(state, "Workflow not loaded");
        }
    }

    private async Task HandleChooseAiAsync(JsonElement root)
    {
         _ = _messageSender.SendActiveStateAsync("ProxyStepId", "x");
        _ = _messageSender.SendActiveStateAsync("AppId", "x");
        var (from, _to, id) = WorkflowTraceEvent.ParseFromToId(Workflow1.TraceAiDoesHumanStepInputEvent) ?? ("", "", "");
        _ = _messageSender.SendActiveStateAsync(id, from);

        var enumerator = _sessionManager.Enumerator;
        var state = _sessionManager.State;
        var workflow = _sessionManager.Workflow;

        if (enumerator != null && state != null)
        {
            workflow?.SetNextIteraction(Workflow1.AIDoesHumanStepEvent);


            await AdvanceWorkflowAndDispatchAsync();
        }
        else
        {
            await _messageSender.SendStateAsync(state, "Workflow not loaded");
        }
    }

    private async Task HandleGetMermaidAsync()
    {
        try
        {
            var workflow = _sessionManager.Workflow;
            var mermaidResult = workflow != null
                ? workflow.GetMermaidGraphFull()
                : null;

            await _messageSender.SendMermaidAsync(
                mermaidResult?.Mermaid,
                mermaidResult?.Nodes ?? new List<AgenticWorkflowSK.NodeInfo>(),
                mermaidResult?.Edges ?? new List<AgenticWorkflowSK.EdgeInfo>()
            );
        }
        catch (Exception ex)
        {
            await _messageSender.SendMermaidAsync(
                "",
                null,
                null,
                ex.Message
            );
        }
    }
}
