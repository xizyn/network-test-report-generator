const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

const sourcePath = path.join(__dirname, '..', 'wps-addin', 'taskpane.js');
const source = fs.readFileSync(sourcePath, 'utf8')
  .replace('const state =', 'globalThis.state =')
  .replace('refreshAll();', '');
const opened = [];
const application = { Documents: { Open: value => opened.push(value) } };
const elements = new Map();
const mappingUpdates = [];
const context = {
  window: { Application: application },
  document: { getElementById: id => {
    if (!elements.has(id)) elements.set(id, { textContent: '', className: '', innerHTML: '', value: '' });
    return elements.get(id);
  } },
  fetch: async (path, options = {}) => {
    if (options.method === 'POST') {
      mappingUpdates.push({ path, body: JSON.parse(options.body) });
      return { ok: true, json: async () => [] };
    }
    return { ok: true, json: async () => [
      { fieldName: '项目名称', value: 'WPS集成测试光伏项目', confirmed: false, status: 'ExactMatched' },
      { fieldName: '客户名称', value: '测试新能源有限公司', confirmed: false, status: 'ExactMatched' }
    ] };
  },
  console
};

async function run() {
  vm.runInNewContext(source, context);
  assert.equal(context.wpsApplication(), application);
  context.openInWps('C:\\reports\\generated.docx');
  assert.deepEqual(opened, ['C:\\reports\\generated.docx']);
  context.state.id = 'project-1';
  elements.set('projects', { textContent: '', className: '', innerHTML: '', value: 'project-1' });
  await context.loadMappings();
  assert.match(elements.get('mappingList').innerHTML, /项目名称/);
  await context.confirmAllMappings();
  assert.deepEqual(mappingUpdates.map(update => update.body), [
    { fieldName: '项目名称', value: 'WPS集成测试光伏项目', confirmed: true },
    { fieldName: '客户名称', value: '测试新能源有限公司', confirmed: true }
  ]);
  console.log('WPS task-pane Application fallback verified');
}

run().catch(error => { console.error(error); process.exitCode = 1; });
