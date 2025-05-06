import React, { useState } from "react";
import ConnectionStatus from "./ConnectionStatus";

type StateUpdate = {
  eventType: string;
  state: any;
  message: string;
  mermaid?: string;
  eventId?: string;
};

type StatusBarProps = {
  connected: boolean;
  showStatePanel: boolean;
  onTogglePanel: () => void;
  stateUpdate?: StateUpdate | null;
};

const StatusBar: React.FC<StatusBarProps> = ({
  connected,
  stateUpdate,
}) => {
  const [showFloatingBox, setShowFloatingBox] = useState(false);

  return (
    <div className="ws-status-bar">
      <ConnectionStatus connected={connected} />
      <button
        onClick={() => setShowFloatingBox((v) => !v)}
        className="workflow-btn toggle statusbar-toggle-btn"
      >
        {showFloatingBox ? "Hide Latest State" : "Show Latest State"}
      </button>
      {showFloatingBox && stateUpdate && (
        <div className="floating-state-content">
          <div className="floating-state-title">Latest State</div>
          <pre className="floating-state-content-text">
            {JSON.stringify(stateUpdate.state, null, 2)}
          </pre>
        </div>
      )}
    </div>
  );
};

export default StatusBar;
