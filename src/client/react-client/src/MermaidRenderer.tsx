// src/client/react-client/src/MermaidRenderer.tsx
import { useEffect, useRef } from "react";
import mermaid from "mermaid";

type MermaidRendererProps = {
  chart: string;
};

let uniqueId = 0;

export default function MermaidRenderer({ chart }: MermaidRendererProps) {
  const ref = useRef<HTMLDivElement>(null);
  const idRef = useRef<string>("");

  if (!idRef.current) {
    uniqueId += 1;
    idRef.current = `mermaid-graph-${uniqueId}`;
  }

  useEffect(() => {
    if (ref.current) {
      mermaid.initialize({ startOnLoad: false });
      mermaid.render(idRef.current, chart)
        .then(({ svg }) => {
          ref.current!.innerHTML = svg;
        })
        .catch(() => {
          ref.current!.innerHTML = "<div style='color:red'>Invalid Mermaid diagram</div>";
        });
    }
  }, [chart]);

  return <div ref={ref} />;
}
