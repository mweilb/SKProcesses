// Renders the workflow graph using the MermaidRenderer component.
// Isolated so that diagram rendering logic is decoupled from the main app UI.
import React from "react";
import MermaidRenderer from "./MermaidRenderer";

type NodeInfo = { id: string; name: string };
type EdgeInfo = { source: string; target: string; label: string };

type ActiveTrailEntry = { activate: string; from: string };

type MermaidDiagramProps = {
  chart: string;
  nodes?: NodeInfo[];
  edges?: EdgeInfo[];
  activeTrail?: ActiveTrailEntry[];
};

import { useEffect, useState } from "react";

const MermaidDiagram: React.FC<MermaidDiagramProps> = ({ chart, nodes = [], edges = [], activeTrail = [] }) => {
  const [diagram, setDiagram] = useState(chart);

  useEffect(() => {
    // Always work with a local copy, never mutate the incoming chart prop
    let baseDiagram = chart && chart.trim().length > 0 ? chart : "graph TD;\nNoGraph;";

    // Ensure diagram type is present at the start
    const diagramTypes = ["graph", "flowchart", "sequenceDiagram", "classDiagram", "stateDiagram", "erDiagram", "journey", "gantt", "pie", "requirementDiagram", "gitGraph", "mindmap", "timeline", "quadrantChart"];
    const hasDiagramType = diagramTypes.some(type => baseDiagram.trim().startsWith(type));
    if (!hasDiagramType) {
      baseDiagram = "graph TD;\n" + baseDiagram;
    }

    // Only add style if chart has real content and nodes/edges are present
    if ((chart && nodes.length > 0) || edges.length > 0) {
      let styleDirectives = `

        style App stroke:#00C853
        style Channel stroke:#00C853
        classDef msftNode fill:#0078D4,stroke:#2B88D8,stroke-width:2px,color:#fff,font-weight:500;
        classDef msftEdgeLabel fill:#107C10,color:#fff,font-weight:500;
        classDef msftEdge stroke:#E81123,stroke-width:2px;
        `;

      nodes.forEach((n) => {
        styleDirectives += `class ${n.id} msftNode;\n`;
      });

      edges.forEach((_e, idx) => {
        styleDirectives += `linkStyle ${idx} stroke:#E81123,stroke-width:2px;\n`;
      });

      // === Active Trail Coloring ===
      // Define a palette of greens from dark to light
      const trailPalette = [
        "#006400", // darkest, most recent
        "#145A32",
        "#228B22",
        "#2E8B57",
        "#32CD32",
        "#5CFF5C",
        "#7CFC00",
        "#A2FF99",
        "#B2FFB2",
        "#E6FFE6"  // lightest, oldest
      ];
      // Define a matching font color palette for each trail color
      const fontPalette = [
        "#fff",    // for #006400
        "#fff",    // for #145A32
        "#fff",    // for #228B22
        "#fff",    // for #2E8B57
        "#222",    // for #32CD32
        "#222",    // for #5CFF5C
        "#222",    // for #7CFC00
        "#222",    // for #A2FF99
        "#222",    // for #B2FFB2
        "#222"     // for #E6FFE6
      ];
      // Get unique node IDs in recency order (most recent last in array)
      const trailNodes: string[] = [];
      activeTrail.forEach(entry => {
        if (entry.activate && !trailNodes.includes(entry.activate)) trailNodes.push(entry.activate);
      });
      // Only keep the last N unique nodes (in recency order)
      const N = trailPalette.length;
      const uniqueTrail = trailNodes.slice(-N);

      // Assign color classes
      uniqueTrail.forEach((nodeId, idx) => {
        // idx: 0 = oldest, N-1 = newest
        const paletteIdx = idx; // newest gets 0
        const color = trailPalette[paletteIdx] || trailPalette[trailPalette.length - 1];
        const fontColor = fontPalette[paletteIdx] || "#222";
        // For the lightest trail (oldest), use blue outline, others use dark outline
        if (paletteIdx === 0) {
          styleDirectives += `classDef trail${paletteIdx} fill:${color},stroke:#2196f3,stroke-width:6px,color:${fontColor},font-weight:700;\n`;
        } else {
          styleDirectives += `classDef trail${paletteIdx} fill:${color},stroke:#333,stroke-width:3px,color:${fontColor},font-weight:700;\n`;
        }
        styleDirectives += `class ${nodeId} trail${paletteIdx};\n`;
      });

 
      // eslint-disable-next-line no-console
      uniqueTrail.forEach((nodeId, idx) => {
        const paletteIdx = idx;
        const color = trailPalette[paletteIdx] || trailPalette[trailPalette.length - 1];
        console.log(`[DEBUG] Node ${nodeId} assigned trail${paletteIdx} (${color})`);
      });

      // Always append style directives at the end for reliability
      baseDiagram = "%%{init: {'theme':'neutral'}}%%" + baseDiagram.trimEnd() + "\n" + styleDirectives.trim();
    }
 

    setDiagram(baseDiagram);
  }, [chart, nodes, edges, activeTrail]);

  return (
    <div>
      <h2>Workflow Graph</h2>
      <MermaidRenderer chart={diagram || "graph TD\nNoGraph;"} />
    </div>
  );
};

export default MermaidDiagram;
