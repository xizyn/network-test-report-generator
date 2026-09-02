const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

const sourcePath = path.join(__dirname, '..', 'wps-addin', 'taskpane.js');
const source = fs.readFileSync(sourcePath, 'utf8').replace('refreshAll();', '');
const opened = [];
const application = { Documents: { Open: value => opened.push(value) } };
const context = {
  window: { Application: application },
  document: { getElementById: () => ({ textContent: '', className: '' }) },
  fetch: async () => ({ ok: true, json: async () => ({}) }),
  console
};

vm.runInNewContext(source, context);
assert.equal(context.wpsApplication(), application);
context.openInWps('C:\\reports\\generated.docx');
assert.deepEqual(opened, ['C:\\reports\\generated.docx']);
console.log('WPS task-pane Application fallback verified');
