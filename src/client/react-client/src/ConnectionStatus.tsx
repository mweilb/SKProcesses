// Shows the current WebSocket connection status to inform the user if the client is connected to the server and can interact.
import React from "react";

type ConnectionStatusProps = {
  connected: boolean;
};

const ConnectionStatus: React.FC<ConnectionStatusProps> = ({ connected }) => (
  <div className={"workflow-status " + (connected ? "connected" : "disconnected")}>
    Status: {connected ? "Connected" : "Disconnected"}
  </div>
);

export default ConnectionStatus;
