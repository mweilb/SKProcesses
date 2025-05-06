 
using Microsoft.SemanticKernel;
 


#pragma warning disable SKEXP0080

namespace AgenticWorkflowSKSample.Workflow1.Steps
{
public class ComputeStep : KernelProcessStep<PropertyBag>
    {
       
        public const string ComputeStepEndedEvent = "ComputeStepEndedEvent";
 
        public const string RequestHumanToFixError = "AskHumanToFixError";

        [KernelFunction]
        public async Task<PropertyBag> DoComputeOnStateAsync(KernelProcessStepContext ctx, PropertyBag state)
        {
            // Step 1: Accept a theme (from state["KeyWord"])


            if (state.TryGetValue<List<string>>("History", out var historyList) && historyList != null)
            {
                if (historyList.Count > 0)
                {
                    await ctx.EmitEventAsync(ComputeStepEndedEvent, data: state, visibility: KernelProcessEventVisibility.Public);
                    return state;
                }
               
            }

            var theme = state.TryGetValue("KeyWord", out var themeObj) && themeObj is string t ? t : null;
            if (string.IsNullOrWhiteSpace(theme))
            {
                // If no theme, emit event to request theme input
                await ctx.EmitEventAsync(RequestHumanToFixError, data: state, visibility: KernelProcessEventVisibility.Public);
                return state;
            }

            state["Theme"] = theme;

            // Step 2: Try to load Contexts/{theme}.md if it exists
            var mdPath = $"src/service/shared/AgenticWorkflowSKSample/Contexts/{theme}.md";
            if (File.Exists(mdPath))
            {
                state["ThemeMarkdownContent"] = await File.ReadAllTextAsync(mdPath);
            }
            else
            {
                state["ThemeMarkdownContent"] = null;
            }

            // Step 3: Pass state to AIFigureOutAction (handled by emitting an event)
            await ctx.EmitEventAsync(ComputeStepEndedEvent, data: state, visibility: KernelProcessEventVisibility.Public);

            return state;
        }
    }

}
