using System.Collections.Generic;

namespace AgenticWorkflowSKSample.Workflow1.Steps
{
    public class AIChoices
    {
        public List<string> Options { get; set; } = new List<string>();
        public int SelectedIndex { get; set; } = 0;
    }
}
