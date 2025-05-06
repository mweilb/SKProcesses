using Microsoft.SemanticKernel;
using System.Text.Json;

#pragma warning disable SKEXP0080

namespace AgenticWorkflowSK
{
    public class WorkflowMessageChannel<T> : IExternalKernelProcessMessageChannel where T : new()
    {
        private readonly Queue<EventInfo> _eventQueue = new();
        public T? State { get; set; } = default!;
        public string ChannelName => "WorkflowMessageChannel";

        public class EventInfo
        {
            public string TopicEvent { get; set; } = string.Empty;
            public string TriggerEventId { get; set; } = string.Empty;
            public T? State { get; set; }
        }

        public ValueTask Initialize()
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask Uninitialize()
        {
            return ValueTask.CompletedTask;
        }

        // Listener delegate and event
        public delegate void ExternalEventEmittedHandler(string externalTopicEvent, KernelProcessProxyMessage message, bool isTraceEvent);
        public event ExternalEventEmittedHandler? OnExternalEventEmitted;

        // Allow external registration of listeners without exposing the event directly
        public void RegisterExternalEventListener(ExternalEventEmittedHandler handler)
        {
            OnExternalEventEmitted += handler;
        }

        public Task EmitExternalEventAsync(string externalTopicEvent, KernelProcessProxyMessage message)
        {
            bool isTrace = WorkflowTraceEvent.IsTraceEvent(externalTopicEvent);

            // Notify listeners
            OnExternalEventEmitted?.Invoke(externalTopicEvent, message, isTrace);

            if (!isTrace)
            {
                var eventInfo = new EventInfo
                {
                    TopicEvent = externalTopicEvent,
                    TriggerEventId = message.TriggerEventId ?? string.Empty,
                    State = GetValue(message.EventData?.ToObject()) is T data ? data : default

                };
                _eventQueue.Enqueue(eventInfo);
            }

            return Task.CompletedTask;
        }


        static T GetValue(object? suggestionsObj)
        {
            if (suggestionsObj is T dict)
                return dict;
            if (suggestionsObj is JsonElement elem && elem.ValueKind == JsonValueKind.Object)
                return elem.Deserialize<T>() ?? new();
            if (suggestionsObj is string json)
                return JsonSerializer.Deserialize<T>(json) ?? new();
            return new();
        }

        // Optional: method to dequeue and process events
        public EventInfo? DequeueEvent()
        {
            if (_eventQueue.Count > 0)
            {
                return _eventQueue.Dequeue();
            }
            return null;
        }
    }
}
