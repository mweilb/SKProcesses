 
using Microsoft.SemanticKernel;
 
 
#pragma warning disable SKEXP0080


namespace AgenticWorkflowSKSample.Workflow1.Steps
{
public class AskAppToDoWorkStep : KernelProcessStep<PropertyBag>
    {

       public const string RequestActivity = "RequestActivity";


        [KernelFunction]
        public async Task<PropertyBag> RequestWorkAsync(
            KernelProcessStepContext ctx,
            Kernel kernel,
            PropertyBag state)
        {

            if (!state.TryGetValue<AIChoices>("Suggestions", out var suggestions) || suggestions == null)
            {
                return state;
            }
 
            await ctx.EmitEventAsync(RequestActivity, data: state);

            return state;
        }
    }
}
