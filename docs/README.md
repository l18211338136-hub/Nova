# docs · 团队协作文档

> 这个目录**入库**，只放「别人需要读」的三类内容：接口契约、后端待办、环境启动。
> 规范、迭代计划、测试截图、任务台账都在维护者本地的 `docs/local/`（已 `.gitignore`），不在此处。

---

## 1. 这里有什么

| 想做什么 | 看这里 |
|:--|:--|
| 后端要补哪些接口、验收怎么做 | [`api/backend-todo.md`](./api/backend-todo.md) |
| 某个接口的字段、错误码、分页语义 | [`api/mcp.md`](./api/mcp.md) |
| 前端怎么在本机跑起来、怎么连 Mock | [`setup/README.md`](./setup/README.md) |

```
docs/
├── README.md            ← 本文件
├── api/
│   ├── backend-todo.md  ← 后端待实现清单 + 验收 + 回写要求
│   └── mcp.md           ← MCP 域契约（服务 / 工具 / 密钥 / 网关）
└── setup/
    └── README.md        ← 环境事实：启动、端口、限制、已知坑
```

**不在**这里的内容（在维护者本地 `docs/local/`）：工程规范 01~12 篇、迭代设计与测试报告、点验截图、任务台账、个人草稿。

---

## 2. 与 `docs/local` 的关系

```
docs/local/（本地工作区，不入库）                 docs/（仓库，入库）
├── designs/engineering-standards/  规范          ├── api/mcp.md          ┐
├── designs/feature-designs/<迭代>/  计划+测试     ├── api/backend-todo.md │ 由脚本提取
├── api/                            契约源 ───────►├── setup/README.md     ┘
├── setup/                          环境事实 ─────►└── README.md（手写）
└── personal-docs / retro           个人区
```

- 🔺 **`docs/` 下的文件是快照，内容以 `docs/local/` 的源文件为准**；直接改这里会在下次发布时被覆盖。
- 维护者流程：改 `docs/local/` 里的源文件 → 跑本地发布脚本（`scripts/publish-docs.mjs`，**脚本未入库**）→ 提交 `docs/`。
- 发布脚本只做一件事：复制这三份 + 把指向本地规范树的链接改成说明文字（规范不随仓库发布）。

---

## 3. 维护者本地操作（`scripts/` 未入库，克隆后没有这两个脚本）

```bash
# 刷新 docs/（docs/local → docs/）
node scripts/publish-docs.mjs

# 备份本地工作区（默认到 D:/dev/backups/nova-docs-local，保留最近 20 份）
bash scripts/backup-docs-local.sh

# 恢复某份备份（在仓库根执行）
tar -xzf "D:/dev/backups/nova-docs-local/docs-local-YYYYmmdd-HHmmss.tar.gz" -C docs
```

> 只读本仓库的协作者不需要跑这些命令；`docs/` 里已是可用内容。

---

## 4. 维护约定

| 时机 | 动作 |
|:--|:--|
| 契约有变（字段/错误码/分页） | 改 `docs/local/api/mcp.md` → 跑发布脚本 → 提交，并在 `changelogs/` 标 `[影响文档]` |
| 后端实现完成 | 更新 `backend-todo.md` 勾选状态与实测结果，未通过的写原因 |
| 环境/端口/启动方式变了 | 改 `docs/local/setup/README.md` → 发布 |
| 新增一个需要协作的域 | 在 `docs/local/api/` 建 `<域>.md`，在发布脚本的 `FILES` 里加一行 |

> 完整规范、历史迭代与截图只在维护者本地工作区（`docs/local/`，不入库）；换机器前先备份，丢了用上面的 `tar` 命令恢复。
