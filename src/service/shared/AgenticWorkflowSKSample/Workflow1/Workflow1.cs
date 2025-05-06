
using Microsoft.SemanticKernel;


using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

using AgenticWorkflowSK;
using AgenticWorkflowSKSample.Workflow1.Steps;
using AgenticWorkflowSKSample.Workflow1.States;
 


#pragma warning disable SKEXP0080
namespace AgenticWorkflowSKSample.Workflow1
{

    public class Workflow1
    {
   
        private static string TraceStartupEvent = string.Empty;
        private static string TraceAIFigureOutActionEvent = string.Empty;

        private static string TraceErrorInComputeEvent = string.Empty;

        private static string TraceHumanIterateStepEvent = string.Empty;
 
        private static string TraceAskAppToDoWorkStepEvent = string.Empty;

        public static string TraceComputeStepInputEvent = string.Empty;
        public static string TraceAiDoesHumanStepInputEvent = string.Empty;   
        public static string TraceInputEventAppToDoWorkStep = string.Empty;


        public const string RequestSystemToDoWork = "RequestSystemInSaveFile";
 
        public const string WaitingOnHumanToFixInput = "HumanFixInput";    
        public const string WaitingOnHumanIterate = "WaitingOnHumanIterate";
 
        public const string AIDoesHumanStepEvent = "AiDoesHumanStepEvent";
 
        static public PromptsConfig LoadConfigurations(string configPath)
        {
 
            PromptsConfig checkerConfig;
            if (!File.Exists(configPath))
            {
                checkerConfig = new PromptsConfig(); // fallback to default if not found
            }
            else
            {
                try
                {
                    var yaml = File.ReadAllText(configPath);
                    var deserializer = new DeserializerBuilder()
                        .IgnoreUnmatchedProperties()
                        .WithNamingConvention(CamelCaseNamingConvention.Instance)
                        .Build();
                    checkerConfig = deserializer.Deserialize<PromptsConfig>(yaml) ?? new PromptsConfig();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading configuration.yml: {ex.Message}");
                    throw;
                }
            }

            return checkerConfig;
        }

        public static (ProcessBuilder, List<KernelProcessEdge>) BuildSteps(PromptsConfig config)
        {
            var builder = new ProcessBuilder("Sample Workflow");

           

            // --- Step Declarations ---
            var computeStep = builder.AddStepFromType<ComputeStep>();

            var aiIterateStep = builder.AddStepFromType<AIFigureOutAction, InputPromptState>(
                new InputPromptState { PromptTemplate = config.AiIteratePromptTemplate }
            );

            var humanInputStep = builder.AddStepFromType<HumanIterateStep>();
            
            var aiDoesHumanStep = builder.AddStepFromType<AIDoesHumanStep, InputPromptState>(
                new InputPromptState { PromptTemplate = config.AIDoesHumanStepPromptTemplate }
            );
           
     
            var askAppToDoWorkStep = builder.AddStepFromType<AskAppToDoWorkStep>();

            TraceStartupEvent = WorkflowTraceEvent.CreateTraceEventName(WorkflowProcess<PropertyBag>.StartEvent, computeStep.Name,computeStep.Id);
            TraceAIFigureOutActionEvent = WorkflowTraceEvent.CreateTraceEventName(ComputeStep.ComputeStepEndedEvent, aiIterateStep.Name, aiIterateStep.Id);
            TraceErrorInComputeEvent = WorkflowTraceEvent.CreateTraceEventName(ComputeStep.RequestHumanToFixError, humanInputStep.Name, humanInputStep.Id);
            TraceHumanIterateStepEvent = WorkflowTraceEvent.CreateTraceEventName("OnFunctionResult", humanInputStep.Name, humanInputStep.Id);
            TraceAskAppToDoWorkStepEvent = WorkflowTraceEvent.CreateTraceEventName(AIDoesHumanStep.AICompleted, askAppToDoWorkStep.Name,askAppToDoWorkStep.Id);    

            TraceComputeStepInputEvent = WorkflowTraceEvent.CreateTraceEventName("App", computeStep.Name,computeStep.Id);    
            TraceAiDoesHumanStepInputEvent = WorkflowTraceEvent.CreateTraceEventName("App", aiDoesHumanStep.Name,aiDoesHumanStep.Id);    
            TraceInputEventAppToDoWorkStep = WorkflowTraceEvent.CreateTraceEventName("App", askAppToDoWorkStep.Name,askAppToDoWorkStep.Id);    

            var eventChannelStep = builder.AddProxyStep([RequestSystemToDoWork, WaitingOnHumanIterate, AIDoesHumanStepEvent,WaitingOnHumanToFixInput,
                                                         TraceStartupEvent,TraceAIFigureOutActionEvent,TraceHumanIterateStepEvent,TraceErrorInComputeEvent, TraceAskAppToDoWorkStepEvent]);
             
            // --- Event Routing (Process Flow) ---

            // Entry point
            builder.OnInputEvent(WorkflowProcess<PropertyBag>.StartEvent)
                .SendEventTo(new(computeStep));



            builder.OnEvent(WorkflowProcess<PropertyBag>.StartEvent)
                .EmitExternalEvent(eventChannelStep, TraceStartupEvent)
                .SendEventTo(new(computeStep));    

            builder.OnInputEvent(AIDoesHumanStepEvent).
                SendEventTo(new(aiDoesHumanStep));    

            computeStep.OnEvent(ComputeStep.RequestHumanToFixError)
                .EmitExternalEvent(eventChannelStep, TraceErrorInComputeEvent)
                .EmitExternalEvent(eventChannelStep, WaitingOnHumanToFixInput);

            computeStep.OnEvent(ComputeStep.ComputeStepEndedEvent)
                .EmitExternalEvent(eventChannelStep, TraceAIFigureOutActionEvent)
                .SendEventTo(new(aiIterateStep));


            aiIterateStep.OnFunctionResult()
                .EmitExternalEvent(eventChannelStep, TraceHumanIterateStepEvent)
                .SendEventTo(new(humanInputStep));

            builder.OnInputEvent(AskAppToDoWorkStep.RequestActivity)
                .SendEventTo(new(askAppToDoWorkStep));



            // Human-in-the-loop event routing
            humanInputStep.OnEvent(HumanIterateStep.RequestHumanInTheLoopForIterate)
                .EmitExternalEvent(eventChannelStep, WaitingOnHumanIterate);

            aiDoesHumanStep.OnEvent(AIDoesHumanStep.AICompleted)
                .EmitExternalEvent(eventChannelStep, TraceAskAppToDoWorkStepEvent)
                .SendEventTo(new(askAppToDoWorkStep));


            askAppToDoWorkStep.OnEvent(AskAppToDoWorkStep.RequestActivity)
                .EmitExternalEvent(eventChannelStep, RequestSystemToDoWork);

            
            List<KernelProcessEdge> externalEdges = [
                new(humanInputStep.Id, new KernelProcessFunctionTarget(askAppToDoWorkStep.Id, AskAppToDoWorkStep.RequestActivity, null, AIDoesHumanStepEvent)),
                new(askAppToDoWorkStep.Id, new KernelProcessFunctionTarget(humanInputStep.Id, HumanIterateStep.RequestHumanInTheLoopForIterate, null, WaitingOnHumanIterate)),
                new(computeStep.Id, new KernelProcessFunctionTarget(computeStep.Id, ComputeStep.RequestHumanToFixError, null, WaitingOnHumanToFixInput)),
       ];
    
   
            return (builder, externalEdges);
        }

     
       
    }
}
