import { useEffect, useRef, useState } from "react";
 
import EventPanel from "./EventPanel";
import MermaidDiagram from "./MermaidDiagram";
import TitleBar from "./TitleBar";
import StatusBar from "./StatusBar";
import "./App.css";

type NodeInfo = { id: string; name: string };
type EdgeInfo = { source: string; target: string; label: string };

type StateUpdate = {
  eventType: string;
  state: any;
  message: string;
  mermaid?: string;
  eventId?: string;
  nodes?: NodeInfo[];
  edges?: EdgeInfo[];
};

function App() {
  // (removed misplaced log here)
  const ws = useRef<WebSocket | null>(null);
  const retryTimeout = useRef<number | null>(null);
  const [connected, setConnected] = useState(false);
  const [stateUpdate, setStateUpdate] = useState<StateUpdate | null>(null);

  // === Active State Trail ===
  const TRAIL_LENGTH = 10; // Settable: number of events to keep in the trail
  type ActiveTrailEntry = { activate: string; from: string };
  const [activeTrail, setActiveTrail] = useState<ActiveTrailEntry[]>([]);
  const activeTrailRef = useRef<ActiveTrailEntry[]>([]);

  useEffect(() => {
    let isUnmounted = false;

    const connect = () => {
      try {
        const socket = new WebSocket("ws://localhost:5000/ws");
        ws.current = socket;

        socket.onopen = () => {
          setConnected(true);
          setStateUpdate(null);
        };
        socket.onclose = () => {
          setConnected(false);
          if (!isUnmounted) {
            retryTimeout.current = setTimeout(connect, 5000);
          }
        };
        socket.onerror = () => {
          setConnected(false);
          if (!isUnmounted) {
            retryTimeout.current = setTimeout(connect, 5000);
          }
        };

        socket.onmessage = (event) => {
          try {
            const data = JSON.parse(event.data);

            // Track active_state events for trail
            if (data.eventType === "active_state") {
              if (data.activate && data.from) {
                activeTrailRef.current = [
                  { activate: data.activate, from: data.from },
                  ...activeTrailRef.current,
                ].slice(0, TRAIL_LENGTH);
                setActiveTrail(activeTrailRef.current);
                }
            }
            else {
            // If this is a Mermaid event, use nodes and edges as sent in the event data
              setStateUpdate(data);
            }
          } catch {
            setStateUpdate({ eventType: "error", state: null, message: "Invalid JSON from server" });
          }
        };
      } catch (err) {
        setConnected(false);
        setStateUpdate({ eventType: "error", state: null, message: "WebSocket connection failed" });
        if (!isUnmounted) {
          retryTimeout.current = setTimeout(connect, 5000);
        }
      }
    };

    connect();

    return () => {
      isUnmounted = true;
      if (ws.current) {
        ws.current.close();
      }
      if (retryTimeout.current) {
        clearTimeout(retryTimeout.current);
      }
    };
  }, []);

  const [keyword, setKeyword] = useState("");
  // (removed misplaced log here)
  // Wrap setKeyword to log all updates
  const setKeywordWithLog = (value: string) => {
    
    setKeyword(value);
  };
  const [lastLoadedTopic, setLastLoadedTopic] = useState<string>("");
  // Place log after all state declarations to avoid ReferenceError
  // This should be after all useState hooks

  const send = (msg: any) => {
    if (ws.current && ws.current.readyState === WebSocket.OPEN) {
      ws.current.send(JSON.stringify(msg));
    }
  };

  const canLoadWorkflow = connected && !!keyword.trim() && keyword !== lastLoadedTopic;

  const handleLoadWorkflow = () => {
    setActiveTrail([]);
    setLastLoadedTopic(keyword);
  };

  // Mermaid diagram state and websocket logic
  const [mermaidChart, setMermaidChart] = useState<string>("");
  const [lastNodes, setLastNodes] = useState<NodeInfo[]>([]);
  const [lastEdges, setLastEdges] = useState<EdgeInfo[]>([]);

  // Request mermaid diagram after websocket connects
  useEffect(() => {
    if (connected && ws.current && ws.current.readyState === WebSocket.OPEN) {
      ws.current.send(JSON.stringify({ action: "get_mermaid" }));
    }
  }, [connected]);

 

  // Handle incoming mermaid diagram
  useEffect(() => {
    if (stateUpdate && stateUpdate.eventType === "mermaid") {
      let md = stateUpdate.mermaid || "";
      // Extract mermaid code block if present
      const match = md.match(/```mermaid\s*([\s\S]*?)```/);
      setMermaidChart(match ? match[1].trim() : md.trim());

      // Persist last non-empty nodes/edges
      if (Array.isArray(stateUpdate.nodes) && stateUpdate.nodes.length > 0) {
        setLastNodes(stateUpdate.nodes);
      }
      if (Array.isArray(stateUpdate.edges) && stateUpdate.edges.length > 0) {
        setLastEdges(stateUpdate.edges);
      }
    }
  }, [stateUpdate]);

  // Track waiting state and timer
  const isWaiting =
    !!stateUpdate?.eventId &&
    stateUpdate.eventId !== "WaitingOnHumanIterate" &&
    stateUpdate.eventType !== "mermaid";

  const [waitSeconds, setWaitSeconds] = useState(0);

  useEffect(() => {
    let timer: number | null = null;
    if (isWaiting) {
      setWaitSeconds(0);
      timer = window.setInterval(() => setWaitSeconds((s) => s + 1), 1000);
    } else {
      setWaitSeconds(0);
    }
    return () => {
      if (timer !== null) clearInterval(timer);
    };
  }, [isWaiting, stateUpdate?.eventId]);

  const [showStatePanel, setShowStatePanel] = useState(false);

return (
  <div className="app-root">
    {/* Top bar */}
<header className="top-bar">
  <TitleBar
    keyword={keyword}
    setKeyword={setKeywordWithLog}
    canLoadWorkflow={canLoadWorkflow}
    onLoadWorkflow={handleLoadWorkflow}
    send={send}
  />
</header>
      {/* Main content */}
      <main className="main-content">
        <div className="workspace">
          <div className="workspace-left">
            
              <EventPanel
                stateUpdate={stateUpdate}
                send={send}
                onSuggestionSelect={(value) => {
                  
                  setKeyword(value);
                }}
              />
          
          </div>
          <div className="workspace-right">
            <MermaidDiagram
              chart={mermaidChart}
              nodes={lastNodes}
              edges={lastEdges}
              activeTrail={activeTrail}
            />
          </div>
        </div>
        <StatusBar
          connected={connected}
          showStatePanel={showStatePanel}
          onTogglePanel={() => setShowStatePanel((v) => !v)}
          stateUpdate={stateUpdate}
        />
      </main>
    </div>
  );
}

export default App;
