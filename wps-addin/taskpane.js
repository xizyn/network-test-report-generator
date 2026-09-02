/* Runs inside the WPS task pane. API calls are real same-origin calls to the Desktop Bridge. */
const state = { id: null, currentDocument: null, mappings: [] };
const headers = { 'Content-Type': 'application/json', 'X-GridReport-Client': 'wps' };
const el = id => document.getElementById(id);

async function api(path, options = {}) {
  const response = await fetch(path, { ...options, headers: { ...headers, ...(options.headers || {}) } });
  const body = await response.json().catch(() => ({}));
  if (!response.ok) throw new Error(body.error || body.errors?.join('\n') || `请求失败：${response.status}`);
  return body;
}
function message(text, isError = false) { el('message').textContent = text; el('message').className = `details ${isError ? 'bad' : 'muted'}`; }
function wpsApplication() {
  if (typeof window !== 'undefined' && window.Application) return window.Application;
  if (typeof Application !== 'undefined') return Application;
  if (typeof window !== 'undefined' && window.wps && window.wps.WpsApplication) return window.wps.WpsApplication();
  return null;
}

async function refreshAll() {
  try {
    const health = await api('/health');
    el('bridge').textContent = `Bridge：已连接 · API ${health.apiVersion}`; el('bridge').className = 'ok';
    const projects = await api('/projects'); const select = el('projects'); const previous = state.id;
    select.innerHTML = projects.length ? projects.map(p => `<option value="${p.id}">${escapeHtml(p.name || '未命名项目')}</option>`).join('') : '<option value="">没有已保存项目</option>';
    state.id = projects.some(p => p.id === previous) ? previous : (projects[0] && projects[0].id); select.value = state.id || '';
    await loadProject(); detectDocument();
  } catch (error) { el('bridge').textContent = 'Bridge：未连接，请启动 GridReport.Desktop'; el('bridge').className = 'bad'; message(error.message, true); }
}

async function loadProject() {
  state.id = el('projects').value || null; if (!state.id) return clearMetrics();
  try {
    const [project, fields, mappings] = await Promise.all([api(`/projects/${state.id}`), api(`/projects/${state.id}/fields`), api(`/projects/${state.id}/mapping`)]);
    el('fields').textContent = fields.length; el('matched').textContent = mappings.filter(x => x.value).length; el('pending').textContent = mappings.filter(x => x.isRequired && !x.confirmed).length; el('template').textContent = project.templatePath ? '已选择' : '未选择';
    el('files').textContent = project.sourceFolder ? '待扫描' : '未选择'; el('errors').textContent = '--';
  } catch (error) { message(error.message, true); }
}

async function scan() {
  if (!state.id) return message('请先在桌面端保存并登记项目。', true);
  try { const result = await api(`/projects/${state.id}/scan`, { method: 'POST' }); el('files').textContent = result.fileCount; el('fields').textContent = result.extractedFieldCount; message(`扫描完成：${result.fileCount} 个文件；提取 ${result.extractedFieldCount} 个候选字段。`); await loadProject(); } catch (error) { message(error.message, true); }
}
async function loadMappings() {
  if (!state.id) return message('请先选择项目。', true);
  try {
    const mappings = await api(`/projects/${state.id}/mapping`);
    state.mappings = mappings;
    renderMappings(mappings);
    message(mappings.length ? '请核对字段值后点击“确认全部已有映射”，正式报告需要人工确认。' : '模板没有批注字段。');
    await loadProject();
  } catch (error) { message(error.message, true); }
}
function renderMappings(mappings) {
  const target = el('mappingList');
  target.innerHTML = mappings.map(mapping => `<div class="mapping-row"><strong>${escapeHtml(mapping.fieldName)}</strong><span>→ ${escapeHtml(mapping.value || '未匹配')}</span><em>${mapping.confirmed ? '已确认' : escapeHtml(mapping.status)}</em></div>`).join('') || '<span class="muted">暂无模板字段。</span>';
}
async function confirmAllMappings() {
  if (!state.id) return message('请先选择项目。', true);
  const mappings = state.mappings.filter(mapping => mapping.value);
  if (!mappings.length) return message('没有可确认的字段，请先扫描资料并读取字段映射。', true);
  try {
    for (const mapping of mappings) {
      await api(`/projects/${state.id}/mapping`, { method: 'POST', body: JSON.stringify({ fieldName: mapping.fieldName, value: mapping.value, confirmed: true }) });
    }
    await loadMappings();
    message(`已确认 ${mappings.length} 个字段映射，可执行正式报告校核。`);
  } catch (error) { message(error.message, true); }
}
async function preflight() {
  if (!state.id) return message('请先选择项目。', true);
  try { const result = await api(`/projects/${state.id}/preflight`, { method: 'POST', body: JSON.stringify({ mode: 'formal' }) }); const issues = [...result.errors, ...result.warnings, ...result.info]; el('errors').textContent = result.errors.length; message(issues.length ? issues.map(x => `[${x.severity}] ${x.message}`).join('\n') : '校核通过，可生成正式报告。', result.errors.length > 0); } catch (error) { message(error.message, true); }
}
async function generate(mode) {
  if (!state.id) return message('请先选择项目。', true);
  try { const result = await api(`/projects/${state.id}/generate`, { method: 'POST', body: JSON.stringify({ mode }) }); message(`报告生成成功\n${result.outputPath}${result.warnings?.length ? `\n警告：${result.warnings.join('；')}` : ''}`); openInWps(result.outputPath); } catch (error) { message(error.message, true); }
}
function detectDocument() {
  try { const app = wpsApplication(); const doc = app && app.ActiveDocument; state.currentDocument = doc && (doc.FullName || (doc.Path && doc.Name ? `${doc.Path}\\${doc.Name}` : null)); el('document').textContent = state.currentDocument ? `已打开：${doc.Name || ''}\n${state.currentDocument}` : '未检测到已打开的 WPS 文字文档。'; } catch (_) { state.currentDocument = null; el('document').textContent = '当前 WPS 版本未能提供活动文档信息。'; }
}
async function setCurrentTemplate() {
  if (!state.id) return message('请先选择项目。', true); detectDocument();
  if (!state.currentDocument || !/\.docx$/i.test(state.currentDocument)) return message('当前文档不是可用的 .docx 模板。', true);
  try { await api(`/projects/${state.id}/template`, { method: 'POST', body: JSON.stringify({ templatePath: state.currentDocument }) }); message('已设为当前报告模板。生成报告仍会复制该模板，不会修改原文件。'); await loadProject(); } catch (error) { message(error.message, true); }
}
function openInWps(path) { try { const app = wpsApplication(); if (app && app.Documents && app.Documents.Open) app.Documents.Open(path); } catch (_) { /* The output path remains visible for manual opening. */ } }
function defineFieldHelp() { message('请在 WPS 中选中占位文本，使用“审阅 → 新建批注”填写字段名；V1 不使用不可靠的 JS API 自动写入批注。'); }
function clearMetrics() { ['files', 'fields', 'matched', 'pending', 'errors', 'template'].forEach(id => { el(id).textContent = '--'; }); }
function escapeHtml(value) { return String(value).replace(/[&<>'"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', "'": '&#39;', '"': '&quot;' }[c])); }
refreshAll();
