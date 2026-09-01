# WPS Local Bridge Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 让 WPS JS Add-in 通过只监听 localhost 的 Bridge 调用现有项目、扫描、字段映射、校核和 DOCX 生成能力。

**Architecture:** `GridReport.Bridge` 是独立的 .NET 8 Kestrel host，仅绑定 `127.0.0.1:43801`，由 Desktop 启动和停止。Bridge 从受控项目索引加载 JSON 项目，再调用既有 Core 扫描器、映射器、校核器与生成器；WPS 任务窗格由 Bridge 同源静态托管。

**Tech Stack:** .NET 8, ASP.NET Core Minimal API, Kestrel, existing GridReport.Core, xUnit.

**Spec:** `docs/architecture.md`

## Global Constraints

- 不监听 `0.0.0.0`，不提供任意文件、命令或删除 API。
- URL 项目 ID 必须是 GUID 且必须来自本地受控项目索引。
- 输出路径只由当前项目的 OutputFolder 和内部生成的安全文件名组成。
- 所有 API 行为先由真实 HTTP 自动化测试覆盖。

---

### Task 1: Bridge 契约与安全项目仓库

**Files:** `src/GridReport.Bridge/BridgeProjectIndex.cs`, `ProjectWorkflowService.cs`, `tests/GridReport.Tests/BridgeTests.cs`。

- [x] 写失败的项目列表、非法 ID、字段与映射读取测试；验证 API 尚不存在。
- [x] 实现持久项目索引以及项目 ID 校验，调用 Core 的 Store/Scanner/Mapping。
- [x] 运行测试确认通过。

### Task 2: localhost API 与报告操作

**Files:** `src/GridReport.Bridge/BridgeHost.cs`, `ApiContracts.cs`, `tests/GridReport.Tests/BridgeTests.cs`。

- [x] 写失败的 health、scan、mapping update、preflight、draft/formal generate、启动/停止测试。
- [x] 实现仅 loopback 的 Kestrel 路由、同源任务窗格与错误响应；未启用通配 CORS。
- [x] 运行测试确认通过。

### Task 3: Desktop 生命周期与 WPS 任务窗格

**Files:** `src/GridReport.Desktop/App.xaml.cs`, `ViewModels/MainViewModel.cs`, `MainWindow.xaml`, `wps-addin/*`。

- [x] 启动 Desktop 时启动 Bridge，端口冲突不崩溃并在 UI 显示地址/状态。
- [x] 将 WPS Ribbon 任务窗格 URL 指向 localhost，并以真实 API 填充项目、扫描、字段、校核与生成。
- [x] 用 Node 语法检查 JavaScript，构建 Release，确认测试通过。
