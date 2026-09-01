# WPS Local Bridge

Desktop 启动时会启动 Bridge：`http://127.0.0.1:43801`。Kestrel 显式绑定 IPv4 loopback，不监听 `0.0.0.0`，因此不会默认暴露到局域网。WPS 任务窗格从该 Bridge 地址加载，API 与网页同源；Bridge 未启用通配 CORS，其他网页默认不能读取接口响应。

## API

| 方法 | 路径 | 作用 |
|---|---|---|
| GET | `/health` | 服务状态、软件版本、API 版本与最近 WPS 请求时间。 |
| GET | `/projects` | 由桌面端保存/打开后登记的项目。 |
| GET | `/projects/{id}` | 项目基本信息。 |
| POST | `/projects/{id}/scan` | 调用 Core 扫描和数据提取。 |
| GET | `/projects/{id}/fields` | 可追溯统一字段。 |
| GET/POST | `/projects/{id}/mapping` | 读取/人工更新模板字段映射。 |
| POST | `/projects/{id}/template` | 将当前 WPS 已打开的存在 `.docx` 设为模板。 |
| POST | `/projects/{id}/preflight` | `draft` 或 `formal` 校核。 |
| POST | `/projects/{id}/generate` | 生成草稿或正式报告副本。 |

项目 ID 必须来自 Bridge 私有项目索引；接口不接受任意读文件、删文件或执行命令请求。模板接口仅接受存在的 `.docx` 文件，输出路径只从当前项目的 OutputFolder 生成。

## 任务窗格

任务窗格通过 `X-GridReport-Client: wps` 标识真实 WPS 请求；它加载项目列表，调用真实扫描、字段读取、校核与生成 API。生成成功后尝试调用 WPS 的文档打开 API；即使当前 WPS 版本不提供该调用，输出路径仍会显示供人工打开。

从 WPS 自动写入批注未启用：当前版本仅在能够读取活动文档路径时将 `.docx` 设为模板。请在 WPS 中使用“审阅 → 新建批注”定义字段，以保持与 Core 的可靠批注解析机制一致。
