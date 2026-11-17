# MCP 后端事项（2026-11-17 更新）

> 给后端：主体已由 `44ccad1` 实现，本文件现在只剩**验收清单**与**两项可选改进**。
> 契约细节以仓库 [`api/mcp.md`](./mcp.md) 为准（该文件已由后端接管维护）。

---

## 0. 状态速览

| 能力 | 实现提交 | 状态 |
|:--|:--|:--|
| `PUT /api/mcp/servers/{id}` + `Mcp.Servers.Update` | `44ccad1` | ✅ 已实现 |
| `McpKey.AllServers` + `McpKeyAccess` 子表 + 迁移 | `44ccad1` | ✅ 已实现 |
| 密钥 Create/Update/查询返回 `allServers` / `scopes` | `44ccad1` | ✅ 已实现 |
| 网关 `tools/list` + `tools/call` 按范围过滤/拦截 | `44ccad1` | ✅ 已实现 |
| 前端 `pnpm gen-api` 切换生成物、删本地适配层 | `44ccad1` | ✅ 已同步 |
| 真实环境验收（只读 7 项：登录/分页/搜索/字段/校验） | `verify-live.mjs` @ 2026-11-17 | ✅ 通过 |
| 真实环境验收（写操作：网关越权） | `verify-live-scope.mjs` @ 2026-11-17 | ✅ 通过（7/7，临时密钥已清理） |
| 工具列表上限/分组（见 §3） | — | 💡 可选 |

---

## 1. 已实现部分（对照用）

### 1.1 服务更新

- 端点：`PUT /api/mcp/servers/{id}`，权限 `Mcp.Servers.Update`
- 请求体：`{ name, baseUrl, swaggerUrl, authToken? }`
- 🔺 `authToken` 缺省或留空 = **沿用原值**（Validator + Handler 已按此实现）
- 前端已改用生成的 `useUpdateMcpServer`

### 1.2 密钥授权范围

| # | 改动 | 落点 |
|:--|:--|:--|
| B-01 | `McpKey.AllServers` | 默认 `true`，兼容旧数据 |
| B-02 | `McpKeyAccess(McpKeyId, ServerId, ToolId?)` | `ToolId = null` = 该服务全部工具 |
| B-03 | 迁移 `AddMcpKeyScope` | 子表 + 索引 + 级联删除 |
| B-04 | Create 校验并写入范围 | `McpKeyScopeHelper.BuildAndValidateAccessesAsync` |
| B-05 | Update 同样处理（不只改名） | 同上，复用同一 Helper |
| B-06 | DTO / 查询投影返回范围 | `McpKeyQueryService` 覆盖 `ToPagedResultAsync` |

校验文案（与前端 V1~V7 一致）：`密钥名称为必填项`、`请至少选择一个 MCP 服务`、`授权范围中存在重复的 MCP 服务`、`所选 MCP 服务不存在或已被删除`、`该服务未选择任何工具`、工具不属于服务。

### 1.3 网关

- `tools/list`：按 `AllServers` 或 `McpKeyAccess` 过滤
- `tools/call`：**独立再校验一次**，越权返回错误文本
- 🔺 只做 list 不做 call 等于没有授权（这条已满足）

---

## 2. 待验收（真实后端）

> 2026-11-17 已完成只读部分（登录、服务/工具列表、`$skip`、`contains` 组合、密钥字段、空范围 400）。
> 下表保留完整清单，标 ✅ 的已实测通过，标 ⏳ 的需写操作或真实大服务。

| # | 用例 | 期望 |
|:--|:--|:--|
| V-1 | `PUT /api/mcp/servers/{id}` 改名 + 改地址 | 200，重开编辑回显新值（⏳ 写操作待做） |
| V-2 | 编辑时 `authToken` 不填 | 原令牌仍可用（未被清空）——✅ **静态已确认，无需实测**：`McpServer.cs:42-46` 的 `if (!string.IsNullOrWhiteSpace(authToken))` 守卫 + 前端留空不传字段，双保险（见 `designs/20261117/05` §6.4）。**但因此令牌无法清空，见 §3.3** |
| V-3 | 只读角色打开服务列表 | 不出现「编辑」；直调 PUT 返回 403 |
| V-4 | 「指定服务 + 部分工具」的 Key 连 SSE，`tools/list` | 只返回范围内工具（⏳ 需建测试密钥） |
| V-5 | 同 Key 直接 `tools/call` 范围外工具 | 被拒（`is not authorized for this key`），**不执行** —— ✅ **已实测通过**（2026-11-17，脚本 `mock/verify-live-scope.mjs`，7/7） |
| V-6 | 旧 Key（`AllServers=true`）连 SSE | 仍可用 |
| V-7 | 密钥 CRUD 后重查列表 | ✅ 读接口已确认返回 `allServers`/`scopes`；写入回显待做 |
| V-8 | 删除密钥 | `McpKeyAccess` 级联删除，无孤儿行 |

---

## 3. 可选改进（不阻塞本轮）

### 3.1 工具列表加保护上限

现状：`/api/mcp/tools` 的 `$top` **缺省 = 全量**（`ToPagedResultAsync` 里 `if (top.HasValue) Take`）。

| 建议 | 理由 |
|:--|:--|
| 加 `MaxTop`（如 1000） | 防止前端误传 `$top=100000` 把库和网络拖垮 |
| 无 `$top` 时给默认页大小（如 200） | 契约更可预期；前端会显式传 `$top`，不受影响 |

> 前端正在按「≤1000 一次拉全，>1000 分页续加载」改造，需要 `$count` 与 `$skip/$top` 保持现有语义。

### 3.2 按组返回工具（仅当单服务工具数很大时）

现状：`McpToolDto` 无 `group` 字段，分组由前端按 `routePath` 推导；分页时**组头计数会不完整**。

若要「任何规模都按组完整展示」，需要后端提供其一：

| 方案 | 形态 | 成本 |
|:--|:--|:--|
| 轻 | `GET /api/mcp/servers/{id}/tool-groups` → `[{ group, count }]` | 小；需与前端 `deriveGroupFromPath` 同规则 |
| 重 | 把 `group` 落库并支持 `$orderby=Group,RoutePath` / `$apply` 聚合 | 大（迁移 + 导入逻辑复用） |

触发条件：出现单服务 > 1000 工具。当前未触发，暂不做。

---

### 3.3 服务认证令牌的「可见」与「可清空」

现状：`McpServer.AuthToken` 只能写入不能清除，且对外完全不可见。

| 问题 | 现状 | 后果 |
|:--|:--|:--|
| 不可清空 | `McpServer.Update` 只在 `authToken` 非空时赋值 | 令牌填错后回不到「无令牌」，只能塞一个错值或重建服务（重建会连带丢工具） |
| 不可观测 | `McpServerDto` 只有 `id/name/baseUrl/swaggerUrl/createdAt` | 列表与编辑框无法提示「已配置令牌」；运维排查只能靠猜；前端也无法安全地做「显示/隐藏」 |

建议（不阻塞本轮，但改动很小）：

| 项 | 建议形态 |
|:--|:--|
| DTO | 增 `hasAuthToken: bool`（**不返回明文**），列表直接可用 |
| 清空 | 请求体增 `clearAuthToken: bool`（或约定 `authToken: ""` 显式清空），与「缺省=不修改」区分开 |
| 前端 | 编辑框 placeholder 显示「已配置，留空不修改」，旁边给「清除令牌」按钮 |

---

## 4. 迁移与风险

| # | 项 | 说明 |
|:--|:--|:--|
| R-1 | 旧 Key 迁移为 `AllServers = true` | 行为**放宽**（原先只能看公开工具）。需业务确认；若要最小权限，改为写入「仅公开工具」的显式 scopes |
| R-2 | 工具被删后范围悬空 | 不级联清理范围，网关过滤时自然不命中；建议删工具时记日志 |
| R-3 | 密钥删除 | 已级联删除 `McpKeyAccess`（V-8 验证） |

---

## 5. 验收后回写

- [ ] `docs/api/mcp.md`：把 V-1~V-8 的实测结论补进「变更记录」
- [ ] `changelogs/`：发版记录，标 `[影响文档] api:mcp / setup / designs:两迭代/05`
- [ ] 迭代 `05-测试报告`：V-1~V-8 结果，未通过的写原因
- [ ] 若采纳 §3.1：更新 `mcp.md` 的分页语义（`MaxTop` / 默认页大小）
