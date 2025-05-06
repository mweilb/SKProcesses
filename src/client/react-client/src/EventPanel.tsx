// Handles the special "WaitingOnHumanIterate" event, providing UI for human-in-the-loop workflow steps.
// This keeps event-specific logic separate from the main UI.
import React, { useEffect, useRef } from "react";

type StateUpdate = {
  eventType: string;
  state: any;
  message: string;
  mermaid?: string;
  eventId?: string;
};

type EventPanelProps = {
  stateUpdate: StateUpdate | null;
  send: (msg: any) => void;
  onSuggestionSelect?: (value: string) => void;
};


const EventPanel: React.FC<EventPanelProps> = ({ stateUpdate, send, onSuggestionSelect }) => {
  console.log("EventPanel rendered, onSuggestionSelect:", typeof onSuggestionSelect);
  const scrollRef = useRef<HTMLDivElement>(null);
  const [selectedIdx, setSelectedIdx] = React.useState<number>(-1);
  const [selectedChoice, setSelectedChoice] = React.useState<string | null>(null);

  // Reset selection state on new message
  useEffect(() => {
    setSelectedIdx(-1);
    setSelectedChoice(null);
  }, [stateUpdate]);

  // Auto-scroll to bottom if already at bottom before update
  useEffect(() => {
    const el = scrollRef.current;
    if (!el) return;
    // Scroll to bottom if already at bottom or at top before update
    const wasAtBottom = el.scrollTop + el.clientHeight >= el.scrollHeight - 1;
    const wasAtTop = el.scrollTop === 0;
    if (wasAtBottom || wasAtTop) {
      requestAnimationFrame(() => {
        if (el) el.scrollTop = el.scrollHeight;
      });
    }
    if (typeof window !== "undefined" && window.getSelection) {
      const sel = window.getSelection();
      if (sel) sel.removeAllRanges();
    }
  }, [stateUpdate]);

  if (!stateUpdate) {
    return <div className="event-panel">No event data available.</div>;
  }
  return (
    <div className="event-panel">
      <h3>Workflow Event: Waiting On Human Iterate</h3>
      <div className="event-panel-scrollable" ref={scrollRef}>
        {/* History */}
        <div>
          <strong>History:</strong>
          <ul>
            {(stateUpdate.state?.History ?? []).map((entry: string, idx: number) => (
              <li key={idx} className="history-entry">{entry}</li>
            ))}
          </ul>
        </div>
        {/* Suggestions */}
        {selectedIdx === -1 ? (
          <div>
            <strong>Suggestions:</strong>
            <div>
              {(stateUpdate.state?.Suggestions?.Options ?? []).map((option: string, idx: number) => (
                <div key={idx} className="event-suggestion-row">
                  <button
                    className={`event-suggestion-btn${selectedIdx === idx ? " selected" : ""}`}
                    onClick={() => {
                      setSelectedIdx(idx);
                      setSelectedChoice(option);
                      if (onSuggestionSelect) {
                        console.log("Setting keyword to:", option);
                        onSuggestionSelect(option);
                        // Dispatch a custom event to focus the input
                        window.dispatchEvent(new Event("focus-keyword-input"));
                      }
                      send({
                        action: "select_suggestion",
                        selectedIndex: idx,
                      });
                    }}
                  >
                    Select
                  </button>
                  <span className="event-suggestion-text">{option}</span>
                </div>
              ))}
            </div>
          </div>
        ) : (
          <div>
            <strong>Selected Choice:</strong>
            <div>{selectedChoice}</div>
          </div>
        )}
        {/* AI Proceed */}
        {selectedIdx === -1 && (stateUpdate.state?.Suggestions?.Options?.length ?? 0) > 0 && (
          <button
            onClick={() => {
              setSelectedIdx(1);
              setSelectedChoice("Let AI choose");
              if (onSuggestionSelect) {
                onSuggestionSelect("Let AI choose");
                window.dispatchEvent(new Event("focus-keyword-input"));
              }
              send({ action: "choose_ai", useAI: true });
            }}
            className="event-ai-btn"
          >
            Let AI Choose
          </button>
        )}
      </div>
    </div>
  );
};

export default EventPanel;
