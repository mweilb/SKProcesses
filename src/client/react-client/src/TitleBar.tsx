import React from "react";
import KeywordInput from "./KeywordInput";

type TitleBarProps = {
  keyword: string;
  setKeyword: (k: string) => void;
  canLoadWorkflow: boolean;
  onLoadWorkflow: () => void;
  send: (msg: any) => void;
};

const TitleBar: React.FC<TitleBarProps> = ({
  keyword,
  setKeyword,
  canLoadWorkflow,
  onLoadWorkflow,
  send,
}) => {
  
  return (
    <div className="title-bar">
      <h1 className="workflow-title">
        Workflow SK Process
      </h1>
      <div className="titlebar-keyword-container">
        <KeywordInput keyword={keyword} setKeyword={setKeyword} />
      </div>
      <button
        onClick={() => {
          send({ action: "load_workflow", yaml: "", keyword });
          setTimeout(() => send({ action: "get_mermaid" }), 100);
          setTimeout(() => send({ action: "advance" }), 200);
          onLoadWorkflow();
        }}
        disabled={!canLoadWorkflow}
        className="workflow-btn load titlebar-load-btn"
      >
        Load Workflow
      </button>
    </div>
  );
};

export default TitleBar;
