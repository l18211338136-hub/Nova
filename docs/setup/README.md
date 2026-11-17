# 环境搭建与启动

> 目标：**从零到能访问的最短路径**，以及这台机器上的已知限制。
> 本文件记「现在是什么样」，不记「应该怎么做」——后者在 `docs/local/designs/engineering-standards/`（维护者本地工作区，不入库）。

---

## 1. 启动路径

| 步 | 命令/操作 | 成功长什么样 |
|:--|:--|:--|
| 1 依赖 | `cd src/Web/admin && pnpm install --frozen-lockfile` | `node_modules` 生成，`node_modules/.pnpm` 存在（isolated 布局） |
| 2 配置 | 新建 `src/Web/admin/.env.development.local`：`VITE_API_URL=`、`API_BASE_URL=http://43.166.2.180:8099` | `pnpm exec vite` 启动后 `/api` 请求被代理到该地址 |
| 3 启动 | `cd src/Web/admin && npx vite --host 0.0.0.0 --port 5173` | 终端打印 `Local: http://localhost:5173/` |
| 4 验证 | `curl --noproxy '*' -o /dev/null -w "%{http_code}" http://127.0.0.1:5173/` 与 `curl --noproxy '*' http://127.0.0.1:5173/api/identity/resolve-tenant` | 页面 200；接口返回 `{"code":200,...,"data":{"tenantIds":[]}}`（是后端信封，不是 SPA HTML） |

> 本地桩（`src/Web/admin/mock/`）**不入库**，仅维护者本机用于离线点验（见 `mock/README-本地接入.md`）。后端接口已全部上线，团队直接用远端，不需要桩。

🔺 前端**不需要**本机跑 .NET 后端；后端在远端 `43.166.2.180:8099`（Kestrel）。仓库 `.env.development` 默认写的是 `https://localhost:7242`（本机后端），不改就会全量 502。

---

## 2. 数据与配置

| 项 | 位置/来源 | 备注 |
|:--|:--|:--|
| 主数据 | 远端 PostgreSQL（后端侧） | 前端不持数据 |
| 本地桩（不入库） | `src/Web/admin/mock/`（业务种子 + 程序化生成的 1200 工具压力服务） | 仅维护者本机；已由 `.git/info/exclude` 排除，换机需手工拷贝 |
| 环境配置 | `src/Web/admin/.env.development`（跟踪）与 `.env.development.local`（个人覆盖，`*.local` 不入库） | 只在 `.local` 里改地址，不要动跟踪的两份 |
| 契约产物 | `src/Web/admin/src/api/endpoints/*`、`src/api/model/*` | 由 `pnpm gen-api` 从运行期 `/openapi/v1.json` 生成，**禁止手改** |

---

## 3. 服务与端口

| 服务 | 端口 | 用途 | 备注 |
|:--|:--|:--|:--|
| Vite dev server | 5173 | 前端 + `/api` 同源代理 | `--host 0.0.0.0` 才能被 127.0.0.1 与局域网访问 |
| Mock 服务 | 8098 | 本地桩（`node mock/server.mjs`） | **维护者本机才有**，团队不需要 |
| 远端后端 | 8099 | 真实 API（Kestrel） | 需要账号密码才能登录 |

`vite.config.ts` 的代理前缀：`/api`、`/nova-storage`、`/uploads`（含 `ws:true`，SignalR 用）。

---

## 4. 已知环境限制

| 限制 | 表现 | 应对 |
|:--|:--|:--|
| 本机没有 .NET 运行时 | `.env.development` 指向 `https://localhost:7242` 时，所有 `/api` 502 | 用 `.env.development.local` 指向远端或 Mock |
| 系统代理（`127.0.0.1:53222`）接管本地请求 | `curl http://127.0.0.1:5173` 超时/502 假象；`git clone` 偶发 502 | 本地请求加 `--noproxy '*'`；git 首次失败可重试或浅克隆 |
| pnpm 在 Windows 下创建符号链接冲突 | `pnpm install` 报 `os error 32（另一个程序正在使用此文件）` | 关闭占用进程 → 删 `node_modules` → `pnpm install --frozen-lockfile` 重装；**不要**改用 `node-linker=hoisted`（pnpm 12 不生效） |
| 未安装 Playwright 浏览器 | `pnpm test`（browser 模式）起不来 | 纯函数单测加 `--browser.enabled=false` 走 Node；点验脚本用 `channel:'chrome'` 复用本机 Chrome |
| 后台进程随会话结束被回收 | 下次打开页面"服务没了" | 需要常驻时在自己的终端里跑 |

---

## 5. 常用验证命令

```bash
cd D:/dev/Nova/src/Web/admin

# 前端探活
curl --noproxy '*' -o /dev/null -w "%{http_code}\n" http://127.0.0.1:5173/

# 代理是否真的到后端（返回 ApiResponse 信封即为通）
curl --noproxy '*' http://127.0.0.1:5173/api/identity/resolve-tenant

# 以下两条仅维护者本机有效（mock/ 不入库，团队跳过）
# Mock 是否活着
curl --noproxy '*' -o /dev/null -w "%{http_code}\n" http://127.0.0.1:8098/api/mcp/servers
# Mock 契约自检（独立端口 8198，不污染 8098 数据）
node mock/self-check.mjs

# 类型 / lint / 纯函数单测
npx tsc -b --pretty false
npx eslint src/features/mcp src/features/mcp-keys
npx vitest run src/features/mcp/lib/mcp-tool-groups.test.ts src/features/mcp-keys/lib/key-scope.test.ts --browser.enabled=false
```

---

## 6. 已知坑

| 现象 | 根因 | 以后怎么做 |
|:--|:--|:--|
| 登录后立刻被踢回登录页，且只看到一条 401 | `main.tsx` 的 `QueryCache.onError` 对**任何** query 401 都清会话跳登录 | 接 Mock 时必须实现 `/api/identity/profile`、`/preferences`、`/menus/me` 等启动期接口；不要开 `MOCK_PROXY=1` 把假 JWT 送到真实后端 |
| 侧边栏整栏空白，页面其余正常 | 菜单唯一数据源是 `GET /api/identity/menus/me`，前端**没有**内置兜底（`sidebar-data.ts` 的 navGroups 未被使用）；菜单内容真源是后端 `seed_menus.json` | 缺接口先补接口；不要按 `src/routes` 猜菜单结构 |
| 请求体在后端解析不出来 | 后端启用 RSA/AES 信封时，body 是密文 | 真实后端按前端加密流程走；Mock 里 `public-key` 返回 `isEnabled:false` 跳过加密 |
| 分页第 2 页开始全错 | OData 用 `$skip`（0 起），部分业务接口用 `page`（1 起），基数不同 | 每个接口单独核对参数语义，不要共用参数对象 |
| `git status` 出现 `routeTree.gen.ts` 改动但 `git diff` 为空 | Vite 的 TanStack Router 插件启动时重写文件，仅换行符差异 | 提交前检查，确无实质 diff 则还原 |
| 点验截图里有"（已编辑）"等被改过的数据 | Mock 只在内存里改，脚本会改名/新增 | 重跑脚本前重启 Mock 还原种子 |

---

## 7. 什么时候更新这份文件

- 启动方式、端口、代理前缀变了
- 换后端地址 / 换账号体系
- 新增一条环境限制，或发现一个**已确诊**的坑
- 换了开发机器或部署环境
