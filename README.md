# Nova

Nova Architecture Project

## EF Core 数据库迁移指南 (Migrations)

本项目采用了 **模块化单体 (Modular Monolith)** 架构。数据库上下文 (`DbContext`) 被拆分到了各个子模块中，而启动程序和配置文件位于 `Nova.WebApi` 中。因此在生成和更新迁移时，必须通过命令行参数明确指定相关项目。

### 1. 为 `MultiTenancy` (多租户框架) 生成和更新迁移

**生成迁移：**
```powershell
dotnet ef migrations add Initial_MultiTenancy -c NovaTenantDbContext -p src\Framework\Nova.Framework.MultiTenancy -s src\Host\Nova.WebApi -o Migrations
```

**更新数据库：**
```powershell
dotnet ef database update -c NovaTenantDbContext -s src\Host\Nova.WebApi
```

### 2. 为 `Identity` (身份模块) 生成和更新迁移

**生成迁移：**
```powershell
dotnet ef migrations add Initial_Identity -c IdentityDbContext -p src\Modules\Identity\Nova.Modules.Identity.Infrastructure -s src\Host\Nova.WebApi -o Migrations
```

**更新数据库：**
```powershell
dotnet ef database update -c IdentityDbContext -s src\Host\Nova.WebApi
```

### 命令参数详解

- **`-c [DbContextName]`** (Context)：指定为哪一个上下文生成迁移。由于项目中存在多个 `DbContext`，必须显式指定。
- **`-p [Project]`** (Project)：指定目标项目（迁移代码文件生成的目录）。
- **`-s [Startup-Project]`** (Startup)：指定启动项目（`Nova.WebApi`）。EF Core 必须启动该项目以读取 `appsettings.json` 中的连接字符串。
- **`-o [Output-Dir]`** (Output)：指定输出文件夹，统一规范放在各模块下的 `Migrations` 文件夹内。