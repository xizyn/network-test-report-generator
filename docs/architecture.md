# 涉网试验报告自动生成系统 V1 架构

## 目标

在不修改原始资料和原始 DOCX 模板的前提下，完成“项目 → 资料扫描 → 可追溯数据 → 模板批注映射 → 人工确认 → 报告副本”的离线闭环。应用只依赖 .NET 和开源 NuGet 包，不依赖 Microsoft Office。

## 组件

- `GridReport.Core`：领域模型、文件类型检测、资料扫描、Excel 提取、模板批注解析、匹配、校核、计算规则、DOCX 与图片写入、日志与项目存储。
- `GridReport.Desktop`：WPF 工程界面；保存项目 JSON、显示扫描结果与字段映射，并调用 Core。
- `GridReport.Tests`：Core 自动化测试以及生成 DOCX 的可重新解析验证。
- `wps-addin`：独立的 WPS JS Add-in 静态任务窗格。它仅连接 `127.0.0.1` 的可选本地 API；独立桌面端不依赖它。

## 数据流

`资料文件夹 → FileTypeDetector → PackageScanner → Extractor → ProjectDataStore(Value + Provenance + Status) → TemplateScanner → FieldMappingEngine → Preflight → TemplateCopyReportGenerator → 输出 DOCX`。

每一个值是 `DataValue`：值、显示文本、来源、提取方式、字段状态和人工确认标志一起保存。报告生成器只读取已选定的映射，不直接读取原始 Excel/PDF。

## DOCX 批注字段

`comments.xml` 的 `w:comment/@w:id` 由正文（以及页眉、页脚）中的 `w:commentRangeStart/@w:id`、`w:commentRangeEnd/@w:id` 和 `w:commentReference/@w:id` 定位。扫描器以 ID 关联批注文本和锚定范围。生成器复制模板，再移除范围内文本节点，并在第一个被替换 Run 中写入新文本和继承的 RunProperties；因此不重建段落、表格、节、页眉或页面布局。跨 Run 范围是受支持路径；跨段落或结构边界被标记为人工处理，避免误填。

## 可靠性与安全

- 所有生成写入唯一的输出副本；原始模板以只读方式打开。
- 预检在 `Missing`、`Conflict`、关键字段未确认、模板异常或输出冲突时阻止正式报告；用户可显式选择草稿。
- 单文件异常变成扫描项状态，不中断其余文件。
- 密码只由调用方临时传入，日志不保存密码或完整敏感值。
- 无法可靠解析的 DWG、FEL、加密文件和扫描 PDF 被归档并给出人工/官方工具工作流。

## 范围边界

V1 可靠支持 OOXML DOCX/XLSX、CSV/TXT、图片、PDF 文本提取和 DOCX 中的批注字段/图片字段。XLS、DOC、WPS/ET、DWG、FEL 以检测、归档、打开/导出引导方式降级；不对其私有二进制格式进行不可信逆向。OCR、CAD 实体解析、FEL 直接解析和 AI 自动填值留给后续版本。
