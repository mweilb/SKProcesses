// Minimal Express server to serve the Mermaid diagram from the CLI client
const express = require('express');
const fs = require('fs');
const path = require('path');
const app = express();
const PORT = 3001;

app.get('/mermaid', (req, res) => {
  const filePath = path.join(__dirname, '../../cli-client/currentgraph.md');
  fs.readFile(filePath, 'utf8', (err, data) => {
    if (err) {
      res.status(404).send('Mermaid diagram not found');
    } else {
      res.type('text/markdown').send(data);
    }
  });
});

app.listen(PORT, () => {
  console.log(`Mermaid server running at http://localhost:${PORT}/mermaid`);
});
