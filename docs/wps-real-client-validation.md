# WPS 真实客户端验收记录

**测试日期：** 2026-09-02  
**Bridge：** `http://127.0.0.1:43801`（API v1，仅 `127.0.0.1`）  
**wpsjs：** 2.2.3（官方 npm 包）  
**加载项：** `gridreportwps`，类型 `wps`，在线 URL `http://127.0.0.1:43801/wps/`

## 环境检查

本机以 PATH、卸载注册表、App Paths、开始菜单、Appx 包、Kingsoft 注册表、当前用户 Kingsoft 目录、桌面窗口，以及 C/D/E/F 盘递归 `wps.exe` 搜索进行了检查。未找到 WPS Office 或 WPS Writer 可执行文件，也没有运行中的 WPS 窗口。

因此 WPS 版本、安装路径和 JS Add-in 客户端支持状态均无法取得。官方加载项本机服务 `http://127.0.0.1:58890/version` 返回“目标计算机积极拒绝”，证明当前会话中没有可供 publish 页面连接的 WPS 客户端服务。

## 已完成的真实部署准备

- 已执行 `npm install -g wpsjs@2.2.3`，并验证 `wpsjs --version`。
- 已执行 `wpsjs build`（在线插件），生成 `wps-addin/wps-addon-build`。
- 已执行 `wpsjs publish --serverUrl http://127.0.0.1:43801/wps/`，生成 `wps-addin/wps-addon-publish/publish.html`。
- Desktop 实际启动后，`/health`、`/wps/ribbon.xml`、`/wps/index.html` 和 `/wps/main.js` 均返回 HTTP 200。
- `ribbon.xml`、`index.html`、`manifest.xml` 与 `main.js` 已按 WPS 在线加载项入口结构补齐；Ribbon 回调使用 `Application.CreateTaskPane(...)`，没有浏览器打开实现。

## 真实客户端结果

| 验收项 | 结果 | 证据 / 原因 |
|---|---|---|
| WPS Writer 与版本识别 | 未通过 | 本机未发现 Writer 可执行文件或客户端服务。 |
| publish 注册与 `publish.xml` | 未通过 | publish 产物已生成，但没有 WPS 服务可执行安装操作。 |
| Ribbon“涉网试验” | 未通过 | 无可运行的 WPS Writer。 |
| WPS 内部任务窗格 | 未通过 | 无可运行的 WPS Writer。 |
| Bridge 连接、项目读取、扫描、字段映射、Preflight | 未通过 | 这些操作必须由真实 WPS 任务窗格发起。 |
| 草稿与正式报告 | 未通过 | 未能从真实 WPS 任务窗格触发。 |
| WPS 打开报告、保存后兼容性 | 未通过 | 无可运行的 WPS Writer。 |
| 真实 WPS 截图 | 未生成 | 不以浏览器截图替代。 |

**真实 WPS 客户端验证未通过。** 原因是当前系统环境中实际没有检测到 WPS Writer，故无法将发布页面注册到 WPS 或执行客户端 UI 验收。

## 可复验步骤

在确认 Writer 已安装且可启动后：

1. 启动 Desktop，确认 Bridge 健康检查为运行状态。
2. 打开 `wps-addin/wps-addon-publish/publish.html`，安装 `gridreportwps`。
3. 重启 WPS Writer，确认“涉网试验”Ribbon；点击“项目”，检查右侧出现“涉网试验报告助手”和“Bridge：已连接 · API v1”。
4. 创建测试项目与 CSV 数据，选择项目并执行扫描、映射确认、Preflight、草稿和正式生成。
5. 用“在 WPS 中打开”打开生成结果，保存、关闭、重开，并用 GridReport 的 DOCX Parser 重新解析。
6. 保存真实 Writer 截图：Ribbon、停靠任务窗格、Bridge 已连接、项目数据、映射、Preflight、生成成功与生成报告打开。
