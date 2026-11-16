# MCP 域接口

> 覆盖 `/api/mcp/*`：MCP 服务、工具、访问密钥，以及第三方客户端用的 MCP 网关。
> 一手契约：运行期 `/openapi/v1.json`；本文件补充 OpenAPI 表达不出的语义与**尚未实现的目標契约**。

## 1. 概述

| 项 | 内容 |
|:--|:--|
| basePath | `/api/mcp` |
| 鉴权 | 管理端点：JWT + `[RequirePermission]`；网关端点（`/sse`、`/messages`）：**仅 MCP Key**，不走 JWT/权限体系 |
| 主要实体 | `McpServer`、`McpTool`（隶属某 Server）、`McpKey`（+ 目标：`McpKeyAccess` 子表） |
| 前端位置 | `src/Web/admin/src/features/mcp`（服务/工具）、`features/mcp-keys`（密钥） |

## 2. 接口清单

| 方法 | 路径 | 用途 | 权限 | 实现状态 |
|:--|:--|:--|:--|:--|
| GET | `/api/mcp/servers` | 服务列表（OData 分页） | `Mcp.Servers.Read` | ✅ |
| PUT | `/api/mcp/servers/{id}` | 修改服务基本信息 | `Mcp.Servers.Update` | ✅ |
| DELETE | `/api/mcp/servers/{id}` | 删除服务（级联删工具） | `Mcp.Servers.Delete` | ✅ |
| GET | `/api/mcp/tools` | 工具列表（OData 分页） | `Mcp.Tools.Read` | ✅ |
| DELETE | `/api/mcp/tools/{id}` | 删除工具 | `Mcp.Tools.Delete` | ✅ |
| POST | `/api/mcp/parse` | 解析 Swagger/OpenAPI 文档为操作清单 | `Mcp.Servers.Import` | ✅ |
| POST | `/api/mcp/import` | 导入选中操作生成服务与工具 | `Mcp.Servers.Import` | ✅ |
| GET | `/api/mcp/keys` | 密钥列表（OData 分页，含范围字段） | `Mcp.Keys.Read` | ✅ |
| POST | `/api/mcp/keys` | 创建密钥（含授权范围） | `Mcp.Keys.Create` | ✅ |
| PUT | `/api/mcp/keys/{id}` | 修改名称与授权范围 | `Mcp.Keys.Update` | ✅ |
| DELETE | `/api/mcp/keys/{id}` | 删除密钥（级联删授权范围） | `Mcp.Keys.Delete` | ✅ |
| GET | `/api/mcp/sse` | 网关：建立 SSE 连接 | MCP Key | ✅ |
| POST | `/api/mcp/messages?sessionId=` | 网关：接收 JSON-RPC（`tools/list`、`tools/call`） | MCP Key | ✅（已执行范围校验） |

## 3. 关键接口

### GET /api/mcp/servers ｜ GET /api/mcp/tools ｜ GET /api/mcp/keys

OData 分页：`$count` `$skip`（**0 起**）`$top` `$orderby` `$filter`。返回 `ApiResponse<PagedResult<T>>`。

- `McpServerDto`：`id` `name` `baseUrl` `swaggerUrl` `createdAt`
- `McpToolDto`：`id` `serverId` `name` `description` `isEnabled` `isPublic` `routePath` `httpMethod` `createdAt`（**无 `group`**，前端按 `routePath` 推导，规则需与后端 `DeriveGroup` 逐字一致）
- `McpKeyDto`：`id` `name` `keyValue` `createdAt` + **目标新增** `allServers` `scopes`

服务详情取该服务的工具：`GET /api/mcp/tools?$filter=ServerId eq {guid}&$top=500&$orderby=RoutePath asc`。

### PUT /api/mcp/servers/{id}

```json
{ "name": "Nova 内部 API 网关", "baseUrl": "http://…", "swaggerUrl": "http://…/openapi/v1.json", "authToken": null }
```

- 校验：`name`、`baseUrl` 必填；`swaggerUrl` 可空。
- 🔺 `authToken` **缺省 = 不修改**；不要用 `null` 覆盖已有令牌，否则一次编辑就清空认证信息。
- 侧效应：改 `baseUrl`/`swaggerUrl` **不自动重建工具**，前端另行提示重新导入。

### 密钥授权范围

```jsonc
// McpKeyDto 字段
{
  "allServers": false,
  "scopes": [
    { "serverId": "8f3a…", "toolIds": null },            // null = 该服务全部工具
    { "serverId": "7d2f…", "toolIds": ["…","…"] }        // 指定工具
  ]
}

// POST /api/mcp/keys 、PUT /api/mcp/keys/{id} 请求体
{ "name": "claude-desktop", "allServers": false, "scopes": [ … ] }
```

| 校验 | 响应 |
|:--|:--|
| `name` 空白 | `code=400` 密钥名称为必填项 |
| `allServers=false` 且 `scopes` 为空 | `code=400` 请至少选择一个 MCP 服务 |
| `serverId` 不存在 / 重复 | `code=400` |
| `toolIds` 为空数组（非 null） | `code=400` 该服务未选择任何工具 |
| `toolIds` 含不属于该服务的工具 | `code=400` |
| `allServers=true` | 忽略并清空 `scopes` |

推荐存储：`McpKey.AllServers` + 子表 `McpKeyAccess(McpKeyId, ServerId, ToolId?)`，`ToolId IS NULL` 表示整服务。

### 网关 `tools/list` / `tools/call`（范围的执行点）

现状：`tools/list` 返回 **`IsEnabled && IsPublic` ∪ 该 Key 创建者自己创建的工具**；`tools/call` 仅按工具名查库，**不校验范围**。

目标：

```text
可见/可调 ⇔ IsEnabled && !IsDeleted && (
    key.AllServers
 || EXISTS(Access a WHERE a.McpKeyId = key.Id AND a.ServerId = t.ServerId
            AND (a.ToolId IS NULL OR a.ToolId = t.Id))
)
```

🔺 **两处都要改**：只过滤 `tools/list` 时，知道工具名即可直接 `tools/call` 越权。`tools/call` 被拒时应返回错误文本 content，不要返回空结果（客户端会重试）。

## 4. 本域错误码

| 码 | 含义 | 客户端怎么处理 |
|:--|:--|:--|
| `400` + message | 校验失败（必填/引用不存在/范围非法） | 展示 message，保持表单内容 |
| `403` | 无 `Mcp.*` 对应权限 | 菜单与按钮按权限隐藏；提示无权限，不刷新 token |
| `404` | 服务/工具/密钥不存在 | 关闭弹窗并 invalidate 列表 |
| `500` | 服务端异常 | 提示重试 |

网关侧不走本表：缺/错 Key 返回 HTTP 401 文本；工具不存在或越权返回 JSON-RPC 内容里的 `Error: …` 文本。

## 5. 变更记录

| 日期 | 变更 | 影响 |
|:--|:--|:--|
| 2026-11-16 | 建域文件：归档服务/工具/密钥/网关的现有契约与两处目标契约（服务更新、密钥授权范围） | 供后端按此实现 B-* 项；前端 Mock 已按此跑通 |
| 2026-11-16 | 后端实现 `PUT /api/mcp/servers/{id}`、密钥授权范围 CRUD 与网关校验，前端已 `pnpm gen-api` 切换生成物 | 所有 ⛔ 待实现项已完成并编译通过 |
