using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nova.Contracts.Exceptions;
using Nova.Modules.Mcp.Application.Common.Interfaces;
using Nova.Modules.Mcp.Application.Dtos;
using Nova.Modules.Mcp.Domain.Entities;

namespace Nova.Modules.Mcp.Application.Keys.Commands;

/// <summary>
/// 密钥授权范围的共享校验与实体构建器，避免 Create / Update 重复实现。
/// </summary>
internal static class McpKeyScopeHelper
{
    public static async Task<List<McpKeyAccess>> BuildAndValidateAccessesAsync(
        IMcpDbContext dbContext,
        Guid keyId,
        string name,
        bool allServers,
        IReadOnlyCollection<McpKeyScopeDto>? scopes,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new NovaValidationException("密钥名称为必填项");
        }

        if (allServers)
        {
            return new List<McpKeyAccess>();
        }

        if (scopes is null || scopes.Count == 0)
        {
            throw new NovaValidationException("请至少选择一个 MCP 服务");
        }

        var serverIds = scopes.Select(s => s.ServerId).ToList();
        if (serverIds.Any(id => id == Guid.Empty))
        {
            throw new NovaValidationException("存在无效的 MCP 服务 ID");
        }

        if (serverIds.Distinct().Count() != serverIds.Count)
        {
            throw new NovaValidationException("授权范围中存在重复的 MCP 服务");
        }

        var existingServerIds = await dbContext.McpServers
            .Where(s => serverIds.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        if (existingServerIds.Count != serverIds.Count)
        {
            throw new NovaValidationException("所选 MCP 服务不存在或已被删除");
        }

        var specificToolIds = new HashSet<Guid>();
        foreach (var scope in scopes)
        {
            if (scope.ToolIds is null) continue;

            if (scope.ToolIds.Count == 0)
            {
                throw new NovaValidationException("该服务未选择任何工具");
            }

            foreach (var toolId in scope.ToolIds)
            {
                specificToolIds.Add(toolId);
            }
        }

        Dictionary<Guid, Guid> toolServerMap = new();
        if (specificToolIds.Count > 0)
        {
            toolServerMap = await dbContext.McpTools
                .Where(t => specificToolIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.ServerId, cancellationToken);
        }

        var accesses = new List<McpKeyAccess>();
        foreach (var scope in scopes)
        {
            if (scope.ToolIds is null)
            {
                accesses.Add(McpKeyAccess.Create(keyId, scope.ServerId, null));
                continue;
            }

            foreach (var toolId in scope.ToolIds.Distinct())
            {
                if (!toolServerMap.TryGetValue(toolId, out var ownerServerId) || ownerServerId != scope.ServerId)
                {
                    throw new NovaValidationException($"工具 {toolId} 不存在或不属于服务 {scope.ServerId}");
                }

                accesses.Add(McpKeyAccess.Create(keyId, scope.ServerId, toolId));
            }
        }

        return accesses;
    }
}
