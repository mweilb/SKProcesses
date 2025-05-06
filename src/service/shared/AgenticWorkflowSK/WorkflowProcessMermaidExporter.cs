using System.Text;
using System.Collections.Generic;
using Microsoft.SemanticKernel;

namespace AgenticWorkflowSK
{
    #pragma warning disable SKEXP0080
    public class WorkflowProcessMermaidExporter
    {
        /// <summary>
        /// Generates a Mermaid flowchart diagram representing the event flow of a KernelProcess.
        /// </summary>
        /// <param name="process">The KernelProcess instance.</param>
        /// <returns>Mermaid diagram as a string.</returns>
        public static MermaidDiagramExportResult GenerateMermaidDiagramFromKernelProcess(KernelProcess process, List<KernelProcessEdge> externalEdges)
        {
            var sb = new StringBuilder();
            sb.AppendLine("flowchart TD");

            var seen = new HashSet<string>();
            var steps = process.Steps;

            var stepIdToStep = new Dictionary<string, string>();
            var nodes = new List<NodeInfo>();
            var edges = new List<EdgeInfo>();

            foreach (var step in steps)
            {
                if (string.IsNullOrWhiteSpace(step.State.Id)) continue;
   
                stepIdToStep[step.State.Id] = step.State.Name;
                nodes.Add(new NodeInfo { Id = step.State.Id, Name = step.State.Name });
            }

            // Add process-level edges
            AddEdgesToMermaidWithExport(process.Edges, sb, seen, edges);

            // Add step-level edges
            foreach (var step in steps)
            {
                AddEdgesToMermaidWithExport(step.Edges, sb, seen, edges);
            }

            string appNode = "App";
            foreach (var node in seen)
            {
                var name = stepIdToStep.TryGetValue(node, out string? value) ? value : "App";
                if (name == "App"){
                    appNode = node;
                    // Ensure "app" node is present with id "app" and name "App"
                    if (!nodes.Any(n => n.Id == "app"))
                    {
                        nodes.Add(new NodeInfo { Id = node, Name = "App" });
                    }
                }
            }

            string proxyStepNode = string.Empty;

            foreach (var step in steps)
            {
                if (step is KernelProcessProxy proxyStep)
                {
                    proxyStepNode = step.State.Id ?? string.Empty;
                    string stepId = step.State.Id ?? "";
                    string edgeLabel = proxyStep.State.Name;
                    string targetId = appNode;

                    var metaData = proxyStep.ProxyMetadata;
                    foreach(var entry in metaData?.EventMetadata ?? [])
                    {
                        if (!WorkflowTraceEvent.IsTraceEvent(entry.Value.TopicName))
                        {
                            var eventName = entry.Value.TopicName;
                            sb.AppendLine($"    {Sanitize(stepId)} -- \"{edgeLabel} => {eventName}\" --> {Sanitize(targetId)}");
                            edges.Add(new EdgeInfo { Source = stepId, Target = targetId, Label = $"{edgeLabel} => {eventName}" });
                        }
                    }

                    foreach(var externalEdge in externalEdges)
                    {
                        string externalStepId = externalEdge.SourceStepId;
                       
                        string functionName = externalEdge.OutputTarget.FunctionName;
                        sb.AppendLine($"    {Sanitize(externalStepId)} -- \"{edgeLabel} => {functionName}\" --> {Sanitize(stepId)}");
                        edges.Add(new EdgeInfo { Source = externalStepId, Target = targetId, Label = $"{edgeLabel} -> {functionName}" });
                         
                    }

                    break;
                }
            }

            // Classify steps for subgraphs (compact LINQ version)
            var proxyStepIds = steps
                .Where(s => s is KernelProcessProxy && !string.IsNullOrWhiteSpace(s.State.Id))
                .Select(s => s.State.Id)
                .Where(id => id != null)
                .Select(id => id!)
                .ToList();

            var appStepIds = steps
                .Where(s => (s.State.Name == "App" || s.State.Id == appNode) && !string.IsNullOrWhiteSpace(s.State.Id))
                .Select(s => s.State.Id)
                .Where(id => id != null)
                .Select(id => id!)
                .ToList();

            var otherStepIds = steps
                .Where(s => !(s is KernelProcessProxy) && s.State.Name != "App" && s.State.Id != appNode && !string.IsNullOrWhiteSpace(s.State.Id))
                .Select(s => s.State.Id)
                .Where(id => id != null)
                .Select(id => id!)
                .ToList();

            // Add any seen nodes not in steps (e.g., "App" node)
            foreach (var node in seen)
            {
                if (!proxyStepIds.Contains(node) && !appStepIds.Contains(node) && !otherStepIds.Contains(node))
                {
                    if (node == appNode)
                        appStepIds.Add(node);
                    else
                        otherStepIds.Add(node);
                }
            }

            RenderSubgraph(sb, "App", appStepIds, stepIdToStep, "App");
            RenderSubgraph(sb, "Channel", proxyStepIds, stepIdToStep, "KernelProcessProxy");
            RenderSubgraph(sb, "Steps", otherStepIds, stepIdToStep, "Step");

            //replace app id with "app"
            sb.Replace(appNode, "AppId");
            if (proxyStepNode != string.Empty)
            {
                sb.Replace(proxyStepNode, "ProxyStepId");
            }
            return new MermaidDiagramExportResult
            {
                Mermaid = sb.ToString(),
                Nodes = nodes,
                Edges = edges
            };
        }

        // Helper to collect edges for export
        private static void AddEdgesToMermaidWithExport(
            IReadOnlyDictionary<string, IReadOnlyCollection<KernelProcessEdge>>? edges,
            StringBuilder sb,
            HashSet<string> seen,
            List<EdgeInfo> exportEdges)
        {
            if (edges == null) return;

            foreach ((var edgeLabelRaw, var edgeList) in edges)
            {
                var edgeLabel = edgeLabelRaw.Contains(".")
                    ? edgeLabelRaw.Substring(edgeLabelRaw.LastIndexOf('.') + 1)
                    : edgeLabelRaw;

                if (edgeList == null) continue;
 
             

                foreach (var edge in edgeList)
                {
                    string stepId = edge.SourceStepId;
                    var target = edge.OutputTarget;

               
                    string targetId = target.StepId;
                    string functionName = target.FunctionName;
                    string parameterName = target.ParameterName ?? "";
                    string eventName = target.TargetEventId ?? "";

                    if (functionName == "EmitExternalEvent")  continue;
     

                    string label;
                    if (!string.IsNullOrWhiteSpace(eventName))
                    {
                        label = $"{edgeLabel} => {eventName}";
                        sb.AppendLine($"    {Sanitize(stepId)} -- \"{label}\" --> {Sanitize(targetId)}");
                    }
                    else
                    {
                        label = $"{edgeLabel} -> {functionName}({parameterName})";
                        sb.AppendLine($"    {Sanitize(stepId)} -- \"{label}\" --> {Sanitize(targetId)}");
                    }
                    seen.Add(stepId);
                    seen.Add(targetId);
                    exportEdges.Add(new EdgeInfo { Source = stepId, Target = targetId, Label = label });
                    
                }
            }
        }

        // DTOs moved below, keep class body here

        // (Implementation will be added here in the next step)

        /// <summary>
        /// Renders a Mermaid subgraph for a group of nodes.
        /// </summary>
        /// <param name="sb">StringBuilder to append to.</param>
        /// <param name="title">Subgraph title.</param>
        /// <param name="nodeIds">Node IDs to include.</param>
        /// <param name="stepIdToStep">Step ID to name mapping.</param>
        /// <param name="defaultName">Default node name if not found.</param>
        private static void RenderSubgraph(
            StringBuilder sb,
            string title,
            List<string> nodeIds,
            Dictionary<string, string> stepIdToStep,
            string defaultName)
        {
            if (nodeIds.Count == 0) return;
            sb.AppendLine($"    subgraph {title}");
            foreach (var node in nodeIds)
            {
                var name = stepIdToStep.TryGetValue(node, out string? value) ? value : defaultName;
                sb.AppendLine($"        {Sanitize(node)}[{name}]");
            }
            sb.AppendLine("    end");
        }

        private static void AddEdgesToMermaid(
            IReadOnlyDictionary<string, IReadOnlyCollection<KernelProcessEdge>>? edges,
            StringBuilder sb,
            HashSet<string> seen)
        {
            if (edges == null) return;

            foreach ((var edgeLabelRaw, var edgeList) in edges)
            {
                var edgeLabel = edgeLabelRaw.Contains(".")
                    ? edgeLabelRaw.Substring(edgeLabelRaw.LastIndexOf('.') + 1)
                    : edgeLabelRaw;

                if (edgeList == null) continue;

                foreach (var edge in edgeList)
                {
                    string stepId = edge.SourceStepId;
                    var target = edge.OutputTarget;

                    string targetId = target.StepId;
                    string functionName = target.FunctionName;
                    string parameterName = target.ParameterName ?? "";
                    string eventName = target.TargetEventId ?? "";

                    if (!string.IsNullOrWhiteSpace(eventName))
                    {
                        sb.AppendLine($"    {Sanitize(stepId)} -- \"{edgeLabel} => {eventName}\" --> {Sanitize(targetId)}");
                      
                    }
                    else{
                        sb.AppendLine($"    {Sanitize(stepId)} -- \"{edgeLabel} -> {functionName}({parameterName})\" --> {Sanitize(targetId)}");
                    }                  
                    seen.Add(stepId);
                    seen.Add(targetId);
                    // Removed duplicate DTO class definitions
                }
            }
        }

        private static string Sanitize(string s)
        {
            // Replace invalid characters for Mermaid node IDs
            return s.Replace(" ", "_").Replace("-", "_");
        }
    }
public class MermaidDiagramExportResult
    {
        public string Mermaid { get; set; } = string.Empty;
        public List<NodeInfo> Nodes { get; set; } = new();
        public List<EdgeInfo> Edges { get; set; } = new();
    }

    public class NodeInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class EdgeInfo
    {
        public string Source { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }
}
