using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace Nova.Framework.Persistence.Interceptors;

/// <summary>
/// 将写入 PostgreSQL <c>timestamp with time zone</c> 列的 DateTime 参数规范化为 UTC。
/// </summary>
/// <remarks>
/// <para>
/// OData（Microsoft.AspNetCore.OData）在解析带时区偏移的日期字面量（如 <c>2026-08-01T00:00:00Z</c>）时，
/// 会先将其转换为“本地墙钟时间”的 <see cref="System.DateTime"/>（<see cref="System.DateTimeKind.Local"/>），
/// 再交给 EF Core 生成查询参数。Npgsql 在写入 <c>timestamp with time zone</c> 时仅接受
/// <see cref="System.DateTimeKind.Utc"/>，否则抛出
/// “Cannot write DateTime with Kind=Local ... only UTC is supported”。
/// </para>
/// <para>
/// OData 在转换时完整保留了瞬时时刻（instant），因此这里用 <see cref="System.DateTime.ToUniversalTime"/>
/// 把本地墙钟时间还原为正确的 UTC 瞬时时刻；对于 <see cref="System.DateTimeKind.Unspecified"/> 的值
/// （本项目约定数据库统一以 UTC 存储）则视为已是 UTC。对于 <c>timestamp without time zone</c> 列不做处理，
/// 以免改变其本地墙钟语义。该拦截器对所有命令幂等，可安全复用。
/// </para>
/// </remarks>
public sealed class UtcDateTimeParameterInterceptor : DbCommandInterceptor
{
    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        NormalizeParameters(command);
        return result;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        NormalizeParameters(command);
        return new ValueTask<InterceptionResult<DbDataReader>>(result);
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        NormalizeParameters(command);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        NormalizeParameters(command);
        return new ValueTask<InterceptionResult<int>>(result);
    }

    public override InterceptionResult<object?> ScalarExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object?> result)
    {
        NormalizeParameters(command);
        return result;
    }

    public override ValueTask<InterceptionResult<object?>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object?> result,
        CancellationToken cancellationToken = default)
    {
        NormalizeParameters(command);
        return new ValueTask<InterceptionResult<object?>>(result);
    }

    private static void NormalizeParameters(DbCommand command)
    {
        foreach (DbParameter parameter in command.Parameters)
        {
            if (parameter is not NpgsqlParameter np) continue;
            if (np.Value is not DateTime dt) continue;
            if (dt.Kind == DateTimeKind.Utc) continue;

            // 仅处理 timestamp with time zone（或 Npgsql 默认推断的 timestamptz）。
            // 对 timestamp without time zone 保持原样，避免改变本地墙钟语义。
            if (np.NpgsqlDbType != NpgsqlTypes.NpgsqlDbType.Unknown &&
                np.NpgsqlDbType != NpgsqlTypes.NpgsqlDbType.TimestampTz)
            {
                continue;
            }

            np.Value = dt.Kind == DateTimeKind.Local
                ? dt.ToUniversalTime()
                : DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        }
    }
}
