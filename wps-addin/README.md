# WPS JS Add-in

此目录是 WPS Writer 在线加载项的完整入口：`ribbon.xml` 定义“涉网试验”选项卡，`index.html` 加载 `main.js`，`manifest.xml` 说明插件元数据；任务窗格由 Desktop Bridge 同源托管在 `http://127.0.0.1:43801/wps/taskpane.html`。

`main.js` 使用 WPS 的全局 `Application.CreateTaskPane(...)` 创建停靠任务窗格，且不会调用 `window.open`、Shell 或默认浏览器。任务窗格调用真实 Bridge API，不含 Mock 数据。

## 本机 publish 部署

1. 启动 GridReport.Desktop，确认 `http://127.0.0.1:43801/health` 可用。
2. 在本目录执行 `wpsjs build`，选择“在线插件”。
3. 执行 `wpsjs publish --serverUrl http://127.0.0.1:43801/wps/`，生成 `wps-addon-build` 和 `wps-addon-publish/publish.html`。
4. 在已运行的 WPS 环境中打开 `publish.html`，点击安装 `gridreportwps`。WPS 官方服务会写入当前用户的 `%APPDATA%\kingsoft\wps\jsaddons\publish.xml`；不要手工编辑 WPS 安装目录或使用过时的 `jsplugins.xml` 默认方案。
5. 关闭并重新打开 WPS Writer，确认顶部出现“涉网试验”，再点击任一按钮打开右侧任务窗格。

若 `CreateTaskPane` 返回空，应记录 WPS 版本、`publish.xml`、加载项服务日志及 URL 安全限制；浏览器能访问地址不代表 WPS 会接受该地址。

不要将项目资料或报告数据开放到局域网：Bridge 只绑定 `127.0.0.1`。
