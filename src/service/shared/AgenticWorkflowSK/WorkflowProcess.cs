using Microsoft.SemanticKernel;

#pragma warning disable SKEXP0080
namespace AgenticWorkflowSK
{
    public class WorkflowProcess<T> where T : new()
    {

        public record ProcessIterationResult(
            T Data,
            string EventId,
            string WhoTriggeredEventId
        );

 
        protected readonly Kernel _kernel;
        protected readonly KernelProcess _kernelProcess;

        protected readonly List<KernelProcessEdge> _externalEdges = [];
        protected readonly WorkflowMessageChannel<T> _messageChannel;
        public WorkflowMessageChannel<T> MessageChannel => _messageChannel;

        public const string StartEvent = "Start";
        public string NextTriggerEventId { get; set; } = string.Empty;

        public WorkflowProcess( Kernel kernel, KernelProcess kernelProcess, List<KernelProcessEdge> externalEdges)
        {
            _kernel = kernel;
            _kernelProcess = kernelProcess;
            _externalEdges = externalEdges ?? [];
            _messageChannel = new WorkflowMessageChannel<T>();
        }
         
 
        public void SetNextIteraction(string nextTriggerEventId)
        {
            NextTriggerEventId = nextTriggerEventId;
        }

        private void ClearInteractions()
        {
             NextTriggerEventId = string.Empty;
        }
 

        public async IAsyncEnumerable<ProcessIterationResult> IterateAsync(T data)
        {
            KernelProcessEvent processEvent = new ()
                {
                    Id = StartEvent,
                    Data = data
                };

            while (true)
            {
                await _kernelProcess.StartAsync(_kernel, processEvent, _messageChannel);

                var eventInfo = _messageChannel.DequeueEvent();

                if (eventInfo == null)
                    break;

                ClearInteractions();
                
                T? state = eventInfo.State;
                if (state == null)
                    break;

                yield return new ProcessIterationResult(state, eventInfo.TopicEvent, eventInfo.TriggerEventId);
    
                if (string.IsNullOrEmpty(NextTriggerEventId))
                    break;

                processEvent = processEvent with
                {
                    Id = NextTriggerEventId,
                    Data = state,
                };
            }
        }

        /// <summary>
        /// Returns a Mermaid diagram representing the KernelProcess graph.
        /// </summary>
        public string GetMermaidGraph()
        {
            return WorkflowProcessMermaidExporter.GenerateMermaidDiagramFromKernelProcess(_kernelProcess, _externalEdges).Mermaid;
        }
/// <summary>
        /// Returns the full MermaidDiagramExportResult for the KernelProcess graph.
        /// </summary>
        public MermaidDiagramExportResult GetMermaidGraphFull()
        {
            return WorkflowProcessMermaidExporter.GenerateMermaidDiagramFromKernelProcess(_kernelProcess, _externalEdges);
        }
    }
}
