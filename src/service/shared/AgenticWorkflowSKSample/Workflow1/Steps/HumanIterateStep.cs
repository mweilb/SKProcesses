 
#pragma warning disable SKEXP0080
 
using Microsoft.SemanticKernel;


namespace AgenticWorkflowSKSample.Workflow1.Steps
{
public class HumanIterateStep : KernelProcessStep<PropertyBag>
    {
   
        public const string RequestHumanInTheLoopForIterate = "RequestHumanInTheLoopIterate";

        
        [KernelFunction]
        public async Task<PropertyBag> IterateWithHumanAsync(PropertyBag state, KernelProcessStepContext ctx)
        {
            await ctx.EmitEventAsync(RequestHumanInTheLoopForIterate, data: state, visibility: KernelProcessEventVisibility.Internal);
            return state;
        }
    }

}
