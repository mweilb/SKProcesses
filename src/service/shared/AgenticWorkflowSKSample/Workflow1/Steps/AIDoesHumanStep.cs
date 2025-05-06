 
using AgenticWorkflowSKSample.Workflow1.States;
using Microsoft.SemanticKernel;
 
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
#pragma warning disable SKEXP0080
namespace AgenticWorkflowSKSample.Workflow1.Steps
{
    public class AIDoesHumanStep : KernelProcessStep<InputPromptState>
    {
        public const string AIToIterate = "AIProcessStep";
        public const string AICompleted = "AIProcessStepCompleted"; 

        private InputPromptState _initialState;

        public AIDoesHumanStep()
        {
            _initialState = new InputPromptState { PromptTemplate = "" };
        }

        public override ValueTask ActivateAsync(KernelProcessStepState<InputPromptState> state)
        {
            _initialState = state.State!;
            return base.ActivateAsync(state);
        }

        [KernelFunction]
        public async Task<PropertyBag> AIToIterateAsync(
            KernelProcessStepContext ctx,
            Kernel kernel,
            PropertyBag state)
        {
     
            // Check if the error keyword is available in ErrorHints
            string theme = "No theme provided.";     
            string history = "No history provided.";     

            if (state.TryGetValue<List<string>>("History", out var historyList) && historyList != null)
            {
                if (historyList.Count > 0)
                {
                    history = string.Join("\n\n", historyList.Select((opt, index) => $"{index + 1}. {opt}"));
                }
               
            }

            if (state.TryGetValue<string>("Theme", out var keyWordObj) && keyWordObj is string keyWord)
            {
                theme = keyWord;
            }

            string optionsString = "1. Only option";;
            if (state.TryGetValue<AIChoices?>("Suggestions", out var suggestions))
            {
                var options = suggestions?.Options ?? new List<string>();
                optionsString = string.Join("\n\n", options.Select((opt, index) => $"{index + 1}. {opt}"));
            }

          
    

            var promptTemplate = _initialState.PromptTemplate;
            var arguments = new KernelArguments
            {
                
                { "theme", theme },
                { "history", history },
                { "options", optionsString },
             };

            var promptTemplateFactory = new HandlebarsPromptTemplateFactory();

            var response = await kernel.InvokePromptAsync(
                promptTemplate,
                arguments,
                templateFormat: HandlebarsPromptTemplateFactory.HandlebarsTemplateFormat,
                promptTemplateFactory: promptTemplateFactory
            );
            var result = response.GetValue<string>() ?? "0";

            int.TryParse(result.Trim(), out int selectedIndex);

            if (state.TryGetValue<PropertyBag>("Suggestions", out var suggestionsDict2) && suggestionsDict2 != null)
            {
                suggestionsDict2["SelectedIndex"] = selectedIndex;
            }

            await ctx.EmitEventAsync(AICompleted, data: state, visibility: KernelProcessEventVisibility.Internal);

            return state;
        }
    }
}
