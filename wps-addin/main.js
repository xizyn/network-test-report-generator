/* WPS JS Add-in entry point. Callbacks are global because ribbon.xml resolves them by name. */
var GridReportAction = '项目';
var GridReportTaskPaneId = '';

function GridReportApplication() {
  if (typeof Application !== 'undefined') return Application;
  if (typeof window !== 'undefined' && window.Application) return window.Application;
  if (typeof wps !== 'undefined' && wps.WpsApplication) return wps.WpsApplication();
  return null;
}

function OnAddinLoad(ribbonUI) {
  var app = GridReportApplication();
  if (app && ribbonUI && !app.ribbonUI) app.ribbonUI = ribbonUI;
  return true;
}

function openGridReportPane(action) {
  GridReportAction = action;
  var app = GridReportApplication();
  if (!app || !app.CreateTaskPane) {
    alert('当前 WPS 版本未提供 JS Add-in 任务窗格 API。');
    return false;
  }
  try {
    var pane = GridReportTaskPaneId && app.GetTaskPane ? app.GetTaskPane(GridReportTaskPaneId) : null;
    if (!pane) {
      // WPS 内部任务窗格；不使用 window.open、Shell 或默认浏览器。
      pane = app.CreateTaskPane('http://127.0.0.1:43801/wps/taskpane.html');
      if (!pane) throw new Error('CreateTaskPane returned undefined.');
      GridReportTaskPaneId = pane.ID || '';
    }
    pane.Visible = true;
    return true;
  } catch (error) {
    alert('无法创建涉网试验任务窗格：' + (error && error.message ? error.message : String(error)));
    return false;
  }
}

function onImportProject() { return openGridReportPane('导入项目'); }
function onProject() { return openGridReportPane('项目'); }
function onScanFiles() { return openGridReportPane('扫描资料'); }
function onDataCenter() { return openGridReportPane('数据中心'); }
function onTemplateFields() { return openGridReportPane('模板字段'); }
function onAutoMatch() { return openGridReportPane('自动匹配'); }
function onPreflight() { return openGridReportPane('报告校核'); }
function onGenerate() { return openGridReportPane('生成报告'); }
function onOutput() { return openGridReportPane('打开输出目录'); }
function onSettings() { return openGridReportPane('设置'); }
