using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Migrations;

namespace Nova.Framework.MultiTenancy.EntityFrameworkCore;

#pragma warning disable EF1001 // Internal EF Core API usage.
public class CustomNpgsqlMigrationsSqlGenerator : NpgsqlMigrationsSqlGenerator
{
    public CustomNpgsqlMigrationsSqlGenerator(
        MigrationsSqlGeneratorDependencies dependencies,
        INpgsqlSingletonOptions npgsqlSingletonOptions)
        : base(dependencies, npgsqlSingletonOptions)
    {
    }

    protected override void Generate(
        CreateTableOperation operation,
        IModel? model,
        MigrationCommandListBuilder builder,
        bool terminate = true)
    {
        // 移除外键约束
        operation.ForeignKeys.Clear();
        base.Generate(operation, model, builder, terminate);
    }

    protected override void Generate(
        AddForeignKeyOperation operation,
        IModel? model,
        MigrationCommandListBuilder builder,
        bool terminate = true)
    {
        // 跳过外键添加
        return;
    }
}
#pragma warning restore EF1001
