# Nova 项目长期记忆

## 项目定位
- **Nova**：.NET 10 模块化单体（Modular Monolith）架构骨架，定位为 AI Agent 平台，但 AI 能力**尚未实现**（仅命名占位）。
- 启动宿主：`src/Host/Nova.WebApi/Program.cs`（唯一可执行入口）。
- 解决方案格式：`.slnx`（非 .sln）。包管理：中央包版本管理 CPM（`Directory.Packages.props`）。

## 已实现 vs 占位
- ✅ 完整实现：**Identity**（用户/角色/菜单/权限/租户登录闭环）。
- ⚠️ 部分实现：**Multitenancy**（建库播种走 Hangfire）、**Notification**（邮件）。
- ❌ 空占位（仅 Api 层一个空 `XxxModule.cs`）：Agent、Chat、MCP、Memory、Model、Workflow、Knowledge、Prompt、Tool、Workspace、Billing、Storage、Audit；以及 Framework 层的 AI/Authorization/EventBus/Messaging/Shared/Storage。

## 关键技术栈
- ORM：EF Core 10；数据库：PostgreSQL（schema 分离 system/identity，全局禁用外键）。
- 多租户：Finbuckle.MultiTenant 10；认证：JWT Bearer；授权：Claim 权限（`[RequirePermission]` + `PermissionFilter`）。
- CQRS：MassTransit Mediator（非 MediatR）；校验：FluentValidation；映射：Mapster；查询：OData（手工 ApplyTo）。
- 缓存：FusionCache（L1 内存 + L2 Redis）；后台任务：Hangfire + PostgreSql。
- 文档：Scalar + OpenAPI；前端：src/Web/admin（React19+Vite+shadcn-admin+Orval 生成 API client）。

## 关键架构约定（重要）
- 模块发现、DI 注册、Consumer/Validator/Endpoint/权限收集**全部靠反射扫描 `Nova.*.dll`**，新增模块零改 Host。
- 声明式端点：`record Command` 上打 `[ApiEndpoint]` + `[RequirePermission]` 即自动生成路由/文档/鉴权/统一响应，无 Controller。
- 权限自发现闭环：反射扫 `[RequirePermission]` 同步代码/数据库/前端三方权限。
- 软删除与多租户过滤器共存：用 EF Core 10 命名查询过滤器 API 避免冲突。
- 领域事件 `IDomainEvent`/`AggregateRoot` 已定义但**零使用**，无 Dispatcher。

## 测试体系（2026-07-31 补齐）
- ✅ **Nova.UnitTests**：58 个全绿。含基础设施 24 个 + **A 档 Handler 11 个**（Menu/Tenant，轻依赖）+ **B 档 Handler 23 个**（Identity 重依赖集成测试，见 `tests/Nova.UnitTests/Handlers/IdentityIntegrationHarness.cs` + `BTrackHandlerTests.cs`）。
- ✅ **Nova.ArchitectureTests**：5 个；**Nova.IntegrationTests**：3 个。三者累计 66 个测试全绿。
- B 档集成 harness 关键约束：Finbuckle 强制要求写系统库/租户库前必须先 `SetTenant`；`IdentityDbContext` 注册时 InMemory 库名固定为宿主租户（新注册用户的 TenantId 仍属新租户，查询需切到新租户上下文）。

## 关键构建 workaround（环境约束，每次跑测试必加）
- 本机 VS 与 Defender/索引器抢 `bin/obj`，`dotnet test` 写 `Nova.UnitTests.xml` 会 CS0016 Access is denied。
- 解法：唯一时间戳 `-p:ArtifactsPath="D:/Github/Nova/artifacts-run-$ts"` + `-p:GenerateDocumentationFile=false` + `--disable-build-servers -p:UseSharedCompilation=false -p:_msCoverageSourceRootTargetName=__none__`。临时产物建议加 `.gitignore`。
- 剩余：无 CI/CD、无 Docker、未引入覆盖率（coverlet.collector 已移除以避免沙箱写入问题）。
- 敏感信息明文硬编码于 `appsettings.json` 与 `NovaIdentityConstants.cs`（JWT 密钥、DB 密码、QQ 邮箱授权码、root 密码 qwe@123!）。
- `DesignTimeDbContextFactoryBase` 查找 `Nova.sln` 但仓库只有 `Nova.slnx`，会 fallback 到 currentDir（潜在 bug）。
- 大量 `catch {}` 静默吞异常；`Nova.WebApi.csproj` 项目引用冗余（通配符后又逐个列出）。

## 运行方式
- 后端：`dotnet run --project src/Host/Nova.WebApi`（需本机 PostgreSQL，可选 Redis）。launchUrl=scalar/v1。
- 前端：`cd src/Web/admin && pnpm install && pnpm dev`（pnpm + Orval）。
- 迁移：启动时自动迁移+播种；手工命令见 README.md。
