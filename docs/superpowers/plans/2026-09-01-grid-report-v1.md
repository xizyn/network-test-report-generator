# 涉网试验报告自动生成系统 V1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 构建离线可运行的 WPF 报告自动化 V1，安全地从客户资料产生可追溯的 DOCX 报告副本。

**Architecture:** Core 保持无 UI 依赖，Desktop 只负责工作流与编辑；OOXML 编辑限于复制模板上的批注锚点，不重建文档。所有数据值均携带来源和状态。

**Tech Stack:** .NET 8, WPF, DocumentFormat.OpenXml, ExcelDataReader, UglyToad.PdfPig, xUnit, Serilog。

**Spec:** `docs/architecture.md`

## Global Constraints

- 只支持 Windows/.NET 8，不使用 Microsoft Office Interop。
- 永远不修改输入资料或模板；正式输出需通过 Preflight。
- 模糊或未确认的关键字段不能进入正式报告。
- 所有新业务行为先写失败的 xUnit 测试。

---

### Task 1: 创建解决方案与领域模型

**Files:** Core 项目、测试项目、Desktop 项目、`Domain/*`。

- [x] 写 DataValue、来源与状态的失败测试；运行并确认失败。
- [x] 实现最小不可变领域模型；运行测试确认通过。

### Task 2: 资料扫描、格式检测与数据提取

**Files:** `Services/FileTypeDetector.cs`, `PackageScanner.cs`, `DataExtractors/*`。

- [x] 写 PNG/伪装 JPG、PDF、OOXML、未知及 CSV 字段提取的失败测试；确认失败。
- [x] 实现文件头、ZIP 包目录和可恢复的扫描；确认测试通过。

### Task 3: 模板批注与字段映射

**Files:** `Template/*`, `Mapping/*`。

- [x] 写带中文批注、重复批注、表格和跨 Run 锚点的失败测试；确认失败。
- [x] 实现 comments.xml 与 ID/范围关联、精确名/别名匹配；确认测试通过。

### Task 4: 报告生成与校核

**Files:** `Reporting/*`, `Validation/*`。

- [x] 写模板不变、替换多处、输出可再打开、缺失字段阻断的失败测试；确认失败。
- [x] 实现安全副本、局部文本写入与 Preflight；确认测试通过（图片字段作为资料归档降级）。

### Task 5: WPF 工作流与持久化

**Files:** `GridReport.Desktop/*`, `ProjectStore.cs`。

- [x] 写项目 JSON 往返保存的失败测试；确认失败。
- [x] 实现项目仪表板、资料、字段、校核和日志页面；确认构建及测试通过。

### Task 6: WPS 集成、文档与发布验证

**Files:** `wps-addin/*`, README, `docs/*`。

- [x] 创建无后端依赖的 WPS Add-in 任务窗格和安装说明。
- [x] 完成支持矩阵、模板/FEL/DWG 操作指南，运行完整测试和 Release 构建。
