# 后端待实现清单（MCP 相关）

> 给后端：这里只有**需要你动手的**内容，契约细节见同目录 [mcp.md](./mcp.md)。
> 来源：两次前端迭代在 Mock 上已跑通的目标契约（`designs/feature-designs/20261113-*`、`20261115-*`）。
> 状态随实现更新；完成后请按 §5 回写文档。

---

## 1. P0：MCP 服务「更新」端点

| 项 | 内容 |
|:--|:--|
| 端点 | `PUT /api/mcp/servers/{id}`（当前不存在，`Servers/Commands/` 只有 Delete） |
| 请求体 | `{ name, baseUrl, swaggerUrl, authToken? }` |
| 校验 | `name`、`baseUrl` 必填；`swaggerUrl` 可空 |
| 🔺 关键 | **`authToken` 缺省 = 不修改**。若用 `null` 覆盖，一次编辑就会清掉该服务的认证令牌（线上事故级） |
| 领域方法 | `McpServer.Update(name, baseUrl, swaggerUrl, authToken)` 已存在，接线即可 |
| 权限码 | **`Mcp.Servers.Update` 目前不存在**，需新增并写进角色授权；否则前端 `hasPermission` 恒 false，「编辑」按钮不显示 |
| 契约 | [mcp.md](./mcp.md) §3「PUT /api/mcp/servers/{id}」 |

前端已按此实现（`features/mcp/hooks/use-update-mcp-server.ts`），端点上线后可换生成物。

## 2. P0：MCP 密钥的「授权范围」

现状：`McpKey` 只有 `Name / KeyValue / ExpiresAt`，**无法限定能访问哪些服务/工具**；网关也没有按 Key 的限制。

| # | 改动 | 要点 |
|:--|:--|:--|
| B-01 | `McpKey` 增 `AllServers: bool` | 默认 `true`（兼容旧数据） |
| B-02 | 新子实体 `McpKeyAccess(McpKeyId, ServerId, ToolId?)` | `ToolId = null` 表示该服务全部工具；级联删除；索引 `(McpKeyId, ServerId, ToolId)` |
| B-03 | EF 配置 + 迁移 | 旧 Key 迁移为 `AllServers = true`（见 §4 风险） |
| B-04 | `CreateMcpKeyCommand` + Handler 增 `AllServers` / `Scopes` | 校验 V1~V7 |
| B-05 | `UpdateMcpKeyCommand` + Handler 同样增字段 | 🔺 **只改 Create 是高频遗漏**，否则编辑保存会把范围清空 |
| B-06 | `McpKeyDto` + 查询投影 | 增 `allServers`、`scopes[{serverId, toolIds}]`（`ToolId=null` → `toolIds: null`） |

契约与校验表：[mcp.md](./mcp.md) §3「密钥授权范围」

## 3. P0：网关按范围执行（安全底线）

| 位置 | 现状 | 目标 |
|:--|:--|:--|
| `McpServerEngine.HandleListToolsAsync` | `IsEnabled && IsPublic` ∪ 该 Key 创建者自己的工具 | 叠加范围判定 |
| `McpServerEngine.HandleCallToolAsync` | 只按工具名查库 | **先校验该 Key 是否有权调用**，无权返回错误文本 |

```text
可访问 ⇔ IsEnabled && !IsDeleted && (
    key.AllServers
 || EXISTS(McpKeyAccess a WHERE a.McpKeyId = key.Id AND a.ServerId = t.ServerId
            AND (a.ToolId IS NULL OR a.ToolId = t.Id))
)
```

🔺 **两处都要改**：只过滤 `tools/list` 时，知道工具名就能直接 `tools/call` 越权，等于没有授权。

## 4. 迁移与风险

| # | 项 | 说明 |
|:--|:--|:--|
| R-1 | 旧 Key 迁移为「全部服务」 | 行为**放宽**（原先只能看公开工具）。需业务确认；若要最小权限，迁移脚本改为写入「仅公开工具」的显式 scopes |
| R-2 | 工具被删后的范围悬空 | 不级联清理范围，网关过滤时自然不命中；建议删工具时记日志 |
| R-3 | 密钥删除 | 子表 `McpKeyAccess` 必须级联删除，避免孤儿行 |

## 5. 完成后的验收与回写

**验收**（对真实后端执行，前端只需改 `API_BASE_URL`）：

| # | 用例 | 期望 |
|:--|:--|:--|
| V-1 | `PUT /api/mcp/servers/{id}` 改名 + 改地址 | 200，重开编辑回显新值 |
| V-2 | 编辑时 `authToken` 不填 | 原令牌仍可用（不是被清空） |
| V-3 | 只读角色打开服务列表 | 不出现「编辑」；直调 PUT 返回 403 |
| V-4 | 用「指定服务 + 部分工具」的 Key 连 SSE，`tools/list` | 只返回范围内工具 |
| V-5 | 同 Key 直接 `tools/call` 一个范围外工具 | 被拒（错误文本），**不执行** |
| V-6 | 旧 Key（迁移后）连 SSE | 仍可用 |
| V-7 | `pnpm gen-api` | 生成物含 `updateMcpServer` 与密钥范围字段 |

**回写文档**（`01-文档体系与更新协议` §3「更新文档」）：

- [ ] `api/mcp.md`：把「⛔ 待实现」改为实际契约（字段/错误码以现状为准）
- [ ] `changelogs/`：写一条发版记录，标 `[影响文档] api:mcp / setup / designs:两迭代/05`
- [ ] 迭代 `05-测试报告`：把 V-1~V-7 的实测结果补上，未通过的写原因
- [ ] 前端：删除 `features/mcp/hooks/use-update-mcp-server.ts` 与 `features/mcp-keys/hooks/use-mcp-key-mutations.ts`，改用 `gen-api` 生成物
