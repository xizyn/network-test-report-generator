/* WPS JS Add-in entry point. WPS loads this file and resolves ribbon.xml callbacks. */
(function () {
  function application() {
    if (typeof wps !== 'undefined' && wps.WpsApplication) return wps.WpsApplication();
    return typeof Application !== 'undefined' ? Application : null;
  }

  window.GridReportAction = '项目';
  window.openGridReportPane = function (action) {
    window.GridReportAction = action;
    var app = application();
    if (!app || !app.CreateTaskPane) throw new Error('当前 WPS 版本未提供 JS Add-in 任务窗格 API。');
    var pane = app.CreateTaskPane('taskpane.html', '涉网试验');
    if (!pane) throw new Error('WPS 拒绝加载任务窗格；请检查加载项签名、地址安全策略和网络访问。');
    pane.Visible = true;
  };

  window.onImportProject = function () { openGridReportPane('导入项目'); };
  window.onScanFiles = function () { openGridReportPane('扫描资料'); };
  window.onDataCenter = function () { openGridReportPane('数据中心'); };
  window.onTemplateFields = function () { openGridReportPane('模板字段'); };
  window.onAutoMatch = function () { openGridReportPane('自动匹配'); };
  window.onPreflight = function () { openGridReportPane('报告校核'); };
  window.onGenerate = function () { openGridReportPane('生成报告'); };
  window.onOutput = function () { openGridReportPane('打开输出目录'); };
  window.onSettings = function () { openGridReportPane('设置'); };
})();
