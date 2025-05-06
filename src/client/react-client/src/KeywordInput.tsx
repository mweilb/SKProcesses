// Handles user input for the workflow keyword, allowing dynamic filtering or selection of workflows.
// This is separated to keep input logic isolated and reusable.
import React, { useEffect, useRef } from "react";

type KeywordInputProps = {
  keyword: string;
  setKeyword: (value: string) => void;
};

const KeywordInput: React.FC<KeywordInputProps> = ({ keyword, setKeyword }) => {
  const inputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    const focusListener = () => {
      inputRef.current?.focus();
    };
    window.addEventListener("focus-keyword-input", focusListener);
    return () => {
      window.removeEventListener("focus-keyword-input", focusListener);
    };
  }, []);

  return (
    <div className="keyword-input-container">
      <label className="keyword-label">
        Topic:
        <input
          ref={inputRef}
          type="text"
          value={keyword}
          onChange={e => setKeyword(e.target.value)}
          className="keyword-input"
          placeholder="Enter topic"
        />
      </label>
    </div>
  );
};

export default KeywordInput;
