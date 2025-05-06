 
#pragma warning disable SKEXP0080
 
using Microsoft.SemanticKernel;


namespace AgenticWorkflowSKSample.Workflow1.Steps
{
public class ValidateStep : KernelProcessStep<PropertyBag>
    {
   
        public const string SuccessValidationEvent = "SuccessValidateStep";
        public const string FaileValidateEvent = "FailureValidateStep";

        
        [KernelFunction]
        public async Task<PropertyBag> ValidateDataAsync(PropertyBag state, KernelProcessStepContext ctx)
        {
            if (state.TryGetValue<AIChoices>("Suggestions", out var suggestions)){
                 await ctx.EmitEventAsync(SuccessValidationEvent, data: state, visibility: KernelProcessEventVisibility.Internal);
            }
            else
            {
                await ctx.EmitEventAsync(FaileValidateEvent, data: state, visibility: KernelProcessEventVisibility.Internal);
            }
            return state;
        }
    }

}
