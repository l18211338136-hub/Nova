---
name: nova-dotnet-test
summary: How to reliably build & run dotnet tests in the Nova repo, and the B-track Identity integration-harness pattern for testing MassTransit IConsumer handlers.
description: >
  Use when adding or running tests in the Nova (.NET 10 modular monolith) repo. Covers:
  (1) the environment build workaround needed to run `dotnet test` without CS0016 / file-lock
  failures (VS + Windows Defender lock bin/obj); (2) the integration-harness pattern for
  testing heavy-dependency Identity handlers (Login/CreateUser/RefreshToken/EmailLogin/Send*Code/
  RegisterUser/Role) with real ASP.NET Identity + EF InMemory + Finbuckle + MassTransit Mediator stub.
---

# Nova .NET Test Workflow

## 1. Build/run workaround (MANDATORY on this machine)

The local Visual Studio (devenv.exe) continuously builds the whole solution and fights
Windows Defender/Indexer for `bin/obj` locks. `dotnet test` then fails with
`CS0016 未能写入 Nova.UnitTests.xml ... Access is denied`.

**Always run tests with this exact invocation:**

```bash
ts=$(date +%s%3N)
dotnet test tests/Nova.UnitTests/Nova.UnitTests.csproj \
  --filter "FullyQualifiedName~BTrackHandlerTests" \
  -p:GenerateDocumentationFile=false \
  -p:ArtifactsPath="D:/Github/Nova/artifacts-run-$ts" \
  --disable-build-servers \
  -p:UseSharedCompilation=false \
  -p:_msCoverageSourceRootTargetName=__none__
```

Key parts:
- `-p:ArtifactsPath="...-<timestamp>"` → unique output dir, avoids bin/obj contention.
- `-p:GenerateDocumentationFile=false` → no `.xml` doc file to lock.
- `--disable-build-servers -p:UseSharedCompilation=false -p:_msCoverageSourceRootTargetName=__none__` → no Roslyn/coverage server files.
- Add `artifacts-run-*` to `.gitignore` (temp build output).
- Do NOT reference `MassTransit.Testing` — NuGet source can't fetch it and B-track doesn't need it.

## 2. B-track integration harness pattern

For heavy-dependency handlers (real ASP.NET Identity, Finbuckle multi-tenant, MassTransit
Mediator), build a minimal container in `tests/Nova.UnitTests/Handlers/`.

### Required fakes (`IdentityIntegrationHarness.cs`)
- `FakeTokenService : ITokenService` — generate real parseable JWT (include `ClaimTypes.NameIdentifier`
  and a `tenantId` claim) so RefreshToken can parse it.
- `FakeNovaCache : INovaCache` — `Dictionary<string,object?>` in-memory.
- `FakeDbInitializer : IDbInitializer` — `MigrateAsync` no-op; `SeedAsync` (called inside an
  already tenant-scoped scope) creates Admin role (guard with `RoleExistsAsync`), creates user,
  `AddToRoleAsync(Admin)`, writes `GlobalUserTenantMapping`.
- `TestTenantDbContext : NovaTenantDbContext` — `OnModelCreating` calls
  `base` then `modelBuilder.Entity<GlobalUserTenantMapping>().HasQueryFilter(null)` so the
  cross-tenant mapping table is readable/writable in InMemory.
- `NoopDataProtectionProvider/NoopDataProtector` — passthrough, just to satisfy
  `AddDefaultTokenProviders()` (UserManager dependency).

### Container assembly (`IdentityIntegrationHarness.Create`)
- `AddHttpContextAccessor()` + `AddMultiTenant<NovaTenantInfo>().WithInMemoryStore()`.
- System DB: `AddDbContext<NovaTenantDbContext>(UseInMemoryDatabase("system-<tid>-<suffix>"))`
  then `AddScoped<NovaTenantDbContext>(sp => new TestTenantDbContext(sp.GetRequiredService<DbContextOptions<NovaTenantDbContext>>()))`.
- Tenant DB: `AddDbContext<IdentityDbContext>(UseInMemoryDatabase("tenant-<tid>-<suffix>"))`.
  NOTE: InMemory DB name is FIXED to the host tenant at registration; new tenants created at
  runtime still land in this same DB but with their own `TenantId` — so queries must switch the
  tenant context to find them.
- `AddIdentityCore<User>(o => o.Password.RequireUppercase=false).AddRoles<Role>()
   .AddEntityFrameworkStores<IdentityDbContext>().AddDefaultTokenProviders()`.
- Register the fakes + `NoopDataProtectionProvider` (singleton) + `IConfiguration` mock whose
  `GetConnectionString("RetailConnection")` returns `"DataSource=:memory:"`.
- `TryAddScoped<ITenantInfo>(sp => sp.GetRequiredService<IMultiTenantContextAccessor>()
   .MultiTenantContext?.TenantInfo!)`.

### Tenant switching
`SetTenant(IServiceProvider sp, NovaTenantInfo? tenant = null)` sets
`sp.GetRequiredService<IMultiTenantContextSetter>().MultiTenantContext =
 new MultiTenantContext<NovaTenantInfo>(tenant ?? CurrentTenant)`.
**Finbuckle enforces a tenant before any SaveChanges on system/tenant DBs** — always SetTenant
before seeding TenantInfo / mappings / users within a scope.

### Reading the response
Handlers call `context.RespondAsync(result)`. Helpers:
- `HandlerTestHarness.CreateConsumeContext(command)` builds the MassTransit `ConsumeContext`.
- `IdentityIntegrationHarness.GetResponded<T>(context)` walks `ReceivedCalls()` for the last
  `RespondAsync` arg and casts to `T`.

### Change-tracker gotcha
Never create a `User` in one scope and call `SetAuthenticationTokenAsync`/`AddToRoleAsync` on it
in another scope — the entity gets double-tracked. Seed the user AND write its auth token in the
SAME scope (see `SeedUserWithRefreshTokenAsync`).

## 3. Real handler bug found & fixed
`CreateUserCommandHandler` originally called `SetPhoneNumberAsync(user, ...)` BEFORE
`CreateAsync(user, password)`. The user wasn't tracked yet, so Finbuckle `EnforceMultiTenant()`
threw `MultiTenantException` on SaveChanges. **Always set phone/claims AFTER `CreateAsync`.**
