# WPS JS Add-in 适配层

此目录是针对 WPS Writer 的自定义 Ribbon 与任务窗格实现。`ribbon.xml` 定义顶部“涉网试验”选项卡，`main.js` 提供九个按钮的回调；回调通过 `CreateTaskPane` 打开 Desktop Bridge 托管的 `http://127.0.0.1:43801/wps/taskpane.html`。任务窗格不是静态演示页：它调用真实 Bridge API。

WPS 的加载项由 `ribbon.xml` 与网页逻辑构成；官方推荐使用 `wpsjs` 的 `publish` 模式进行部署，而非手工修改 WPS 安装目录。请参照 [WPS 加载项开发说明](https://open.wps.cn/documents/app-integration-dev/wps365/client/wpsoffice/wps-integration-mode/wps-addin-development/wps-addin-development-instructions) 使用 WPS JS 工具包构建/发布本目录；用户通过发布产生的 `publish.html` 安装。官方文档说明 Windows 企业版的 `publish` 模式需要满足其列出的版本条件。

## 安装与当前边界

- 使用 WPS 官方 `wpsjs publish` 工作流构建此目录，部署生成的加载项文件，并通过生成的 `publish.html` 安装。启动 Desktop 后再打开 WPS Writer，顶部应出现“涉网试验”选项卡。
- Ribbon 与任务窗格代码已就绪，但当前开发机未安装 WPS，未做实际加载验证或本机注册。
- 任务窗格探测 `/health` 并在同源下调用 `/projects`、扫描、字段、映射、校核和生成接口。独立 WPF 应用不依赖 WPS 加载项。
- 若 `CreateTaskPane` 返回空，WPS 官方 API 将其视为加载失败或 URL 安全检查失败；应先检查 WPS 加载项部署与地址策略。

不要将项目资料或报告数据开放到局域网：桥接服务只应绑定 `127.0.0.1`。
