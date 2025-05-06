// Displays the latest state update from the server for transparency and debugging.
// Isolated to allow easy modification of state display logic in the future.
import React from "react";

type StateUpdate = {
  eventType: string;
  state: any;
  message: string;
  mermaid?: string;
  eventId?: string;
};

type StatePanelProps = {
  stateUpdate: StateUpdate | null;
};

const StatePanel: React.FC<StatePanelProps> = ({ stateUpdate }) => (
  <div className="state-panel">
    <div className="state-panel-title">Latest State</div>
    <pre>
      {stateUpdate ? JSON.stringify(stateUpdate, null, 2) : "No state yet"}
    </pre>
  </div>
);

export default StatePanel;
