 
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using AgenticWorkflowSKSample.Workflow1.States;
 
#pragma warning disable SKEXP0080

namespace AgenticWorkflowSKSample.Workflow1.Steps
{

    public class AIFigureOutAction : KernelProcessStep<InputPromptState>
    {
        private InputPromptState _initialState;

        public AIFigureOutAction()
        {
            _initialState = new InputPromptState { PromptTemplate = "" };
        }

        public override ValueTask ActivateAsync(KernelProcessStepState<InputPromptState> state)
        {
            _initialState = state.State!;
            return base.ActivateAsync(state);
        }


        [KernelFunction]
        public async Task<PropertyBag> DoSomeActionAsync(
            KernelProcessStepContext ctx,
            Kernel kernel,
            PropertyBag state)
        {

            try{
                // Check if the error keyword is available in ErrorHints
                string theme = "No theme provided.";     
                string history = "No history provided.";     

                if (state.TryGetValue<List<string>>("History", out var historyList) && historyList != null)
                {
                    if (historyList.Count > 0)
                    {
                        history = string.Join("\n", historyList.Select((opt, index) => $"{index + 1}. {opt}"));
                    }
                
                }

                if (state.TryGetValue<string>("Theme", out var keyWordObj) && keyWordObj is string keyWord)
                {
                    theme = keyWord;
                }


                var promptTemplate = _initialState.PromptTemplate;
                var arguments = new KernelArguments
                {
                    { "stateId", state.ContainsKey("StateId") ? state["StateId"] : 0 },
                    { "theme", theme },
                    { "history", history },
                };

                var promptTemplateFactory = new HandlebarsPromptTemplateFactory();

                var response = await kernel.InvokePromptAsync(
                    promptTemplate,
                    arguments,
                    templateFormat: HandlebarsPromptTemplateFactory.HandlebarsTemplateFormat,
                    promptTemplateFactory: promptTemplateFactory
                );

                var result = PropertyBag.CleanCodeBlock(response.GetValue<string>() ?? "");

                var suggestions = new List<string>();

                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(result);
                    if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        suggestions = doc.RootElement.EnumerateArray().Select(e => e.GetString() ?? "").ToList();
                    }
                    else if (doc.RootElement.TryGetProperty("suggestions", out var suggestionsElement) && suggestionsElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        suggestions = suggestionsElement.EnumerateArray().Select(e => e.GetString() ?? "").ToList();
                    }
                }
                catch (System.Text.Json.JsonException)
                {
                    suggestions = result.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()).ToList();
                }

                state.Set("Suggestions", new AIChoices()
                {
                    Options = suggestions,
                    SelectedIndex = 0
                });
            }catch (Exception ex){
                // Handle the exception as needed
                Console.WriteLine($"Error in AIFigureOutAction: {ex.Message}");
                // You can also log the exception or take other actions as needed
            }            
    
            return state;
        }
    }

    // RecommendationResult class is no longer needed.
}
