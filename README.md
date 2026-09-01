# 涉网试验报告自动生成系统 V1

面向光伏涉网试验工程师的 Windows 离线资料整理与 DOCX 报告生成工具。它不依赖 Microsoft Office 或 Interop，使用 OOXML 精确读取 WPS/Word 模板批注，并始终在模板副本上操作。

## 已实现

- 项目 JSON 保存/打开，保存字段、来源和人工修改审计记录。
- 递归扫描资料；通过文件头、ZIP 内容与文本特征识别真实文件类型，提示扩展名异常。
- XLSX/XLS、CSV/TXT 的首列字段—次列值提取，记录工作表与单元格来源。
- DOCX 批注字段扫描，按 comment ID 处理普通段落、表格内和跨 Run 的批注范围。
- 精确匹配、别名建议、重复字段多目标写入、可编辑的字段确认表。
- 草稿/正式报告 Preflight，原模板保护、输出不静默覆盖、局部文本替换与日志。
- WPF 桌面工作台及 WPS 任务窗格适配层（详见 `wps-addin/`）。
- Desktop 自动启动 localhost WPS Bridge；任务窗格通过同源 API 调用扫描、字段、校核和报告生成（详见 `docs/wps-bridge.md`）。

## 环境与编译

- Windows 10/11
- .NET SDK 8.0 或更高版本
- WPS Office（可选；用于人工打开、制作模板和加载插件）

```powershell
cd 'C:\Users\30264\软件项目\涉网试验报告自动生成系统'
dotnet restore GridReport.sln
dotnet test GridReport.sln
dotnet run --project src\GridReport.Desktop\GridReport.Desktop.csproj
```

发布独立 Windows 目录：

```powershell
dotnet publish src\GridReport.Desktop\GridReport.Desktop.csproj -c Release -r win-x64 --self-contained false -o .\publish\win-x64
```

## 第一次真实测试

1. 用 WPS 创建一份脱敏 DOCX 模板；选中 `XXXXXXXX`，添加批注，例如 `项目名称`。
2. 准备资料目录和 UTF-8 CSV，例如 `项目名称,XX光伏涉网试验项目`；也可放入 XLSX。
3. 启动桌面端，点“新建项目”，填写信息与输出目录后保存项目。
4. 选择资料目录并执行“扫描资料”；检查“需要人工确认”的文件。
5. 选择 DOCX 模板，进入“字段映射”，确认自动结果或手工输入并勾选“确认”。
6. 先生成草稿，使用 WPS 打开并核对格式与字段位置。
7. 所有关键字段确认后再生成正式 DOCX。原模板的文件时间与内容不会被程序写入。

## 安全原则

- 绝不修改原始模板、客户资料或已存在的输出文件。
- 密码不持久化、不写入日志。
- 自动建议不是正式数据；正式报告要求关键字段人工确认。
- 单个文件失败只会产生预检记录，不会中断整个资料包扫描。

## 文档

- [架构](docs/architecture.md)
- [文件支持矩阵](docs/file-support.md)
- [模板制作指南](docs/template-guide.md)
- [FEL 工作流](docs/fel-workflow.md)
- [DWG 工作流](docs/dwg-workflow.md)
- [WPS 适配层](wps-addin/README.md)
- [WPS Bridge API](docs/wps-bridge.md)

## 依赖与许可证

| 包 | 用途 | 许可证 |
|---|---|---|
| DocumentFormat.OpenXml | DOCX/XLSX OOXML 操作 | MIT |
| ExcelDataReader | XLS/XLSX 数据读取 | MIT |
| Serilog / File Sink | 不记录密码的结构化日志 | Apache-2.0 |
| xUnit | 自动化测试 | Apache-2.0 |

PDF、DWG、FEL 的 V1 降级方式见文件支持矩阵；没有使用来源不明二进制文件或商业破解组件。
