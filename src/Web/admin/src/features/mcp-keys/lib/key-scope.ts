import { DEFAULT_SCOPE, type McpKeyRow, type McpKeyScope, type McpKeyScopeValue } from '../types'

/**
 * 密钥授权范围的纯函数集合。
 *
 * 全部保持**不可变**（返回新对象），便于 React 受控组件与单测；不做 IO、不读全局。
 * 校验规则对齐 `02-系统设计` §2.3（V2/V5），前端只做体验拦截，后端仍须校验。
 */

export type ScopeValidationError =
  | { code: 'no-server' }
  | { code: 'no-tool'; serverId: string }

/** `allServers = true` 时清空并忽略 scopes，避免把脏范围提交给后端。 */
export function normalizeScope(value: McpKeyScopeValue): McpKeyScopeValue {
  if (value.allServers) return { allServers: true, scopes: [] }
  return {
    allServers: false,
    scopes: value.scopes.map((scope) => ({
      serverId: scope.serverId,
      toolIds: scope.toolIds === null ? null : [...scope.toolIds],
    })),
  }
}

/** 前端校验：指定模式至少 1 个服务；每个服务要么全部工具，要么至少 1 个工具。 */
export function validateScope(value: McpKeyScopeValue): ScopeValidationError | null {
  if (value.allServers) return null
  if (value.scopes.length === 0) return { code: 'no-server' }
  const empty = value.scopes.find((s) => Array.isArray(s.toolIds) && s.toolIds.length === 0)
  if (empty) return { code: 'no-tool', serverId: empty.serverId }
  return null
}

export type ScopeSummary =
  | { kind: 'all' }
  | { kind: 'servers'; servers: number; tools: number; allTools: boolean }
  | { kind: 'none' }

/** 列表列摘要：全部服务 / N 个服务 · 全部工具 / N 个服务 · M 个工具。 */
export function summarizeScope(value: McpKeyScopeValue): ScopeSummary {
  if (value.allServers) return { kind: 'all' }
  if (value.scopes.length === 0) return { kind: 'none' }

  const allTools = value.scopes.every((s) => s.toolIds === null)
  const tools = value.scopes.reduce(
    (sum, s) => sum + (Array.isArray(s.toolIds) ? s.toolIds.length : 0),
    0
  )
  return { kind: 'servers', servers: value.scopes.length, tools, allTools }
}

/** 勾选/取消一个服务；新勾选的服务默认「全部工具」。 */
export function toggleServer(
  value: McpKeyScopeValue,
  serverId: string,
  selected: boolean
): McpKeyScopeValue {
  const existing = value.scopes.find((s) => s.serverId === serverId)
  if (selected) {
    if (existing) return value
    return { ...value, scopes: [...value.scopes, { serverId, toolIds: null }] }
  }
  if (!existing) return value
  return { ...value, scopes: value.scopes.filter((s) => s.serverId !== serverId) }
}

/** 在「全部工具」与「指定工具」之间切换；`allToolIds` 为切到指定模式时的默认勾选。 */
export function setServerAllTools(
  value: McpKeyScopeValue,
  serverId: string,
  allTools: boolean,
  allToolIds: string[] = []
): McpKeyScopeValue {
  return {
    ...value,
    scopes: value.scopes.map((s) =>
      s.serverId === serverId
        ? { ...s, toolIds: allTools ? null : [...allToolIds] }
        : s
    ),
  }
}

/** 勾选/取消单个工具。仅对「指定工具」的服务生效（null 时不处理）。 */
export function toggleTool(
  value: McpKeyScopeValue,
  serverId: string,
  toolId: string
): McpKeyScopeValue {
  return {
    ...value,
    scopes: value.scopes.map((s) => {
      if (s.serverId !== serverId) return s
      if (s.toolIds === null) return s
      const has = s.toolIds.includes(toolId)
      return {
        ...s,
        toolIds: has ? s.toolIds.filter((id) => id !== toolId) : [...s.toolIds, toolId],
      }
    }),
  }
}

/** 批量设置某服务的工具集合（组级全选/清空用）。 */
export function setServerTools(
  value: McpKeyScopeValue,
  serverId: string,
  toolIds: string[]
): McpKeyScopeValue {
  return {
    ...value,
    scopes: value.scopes.map((s) =>
      s.serverId === serverId ? { ...s, toolIds: [...toolIds] } : s
    ),
  }
}

/**
 * 清理已不存在的服务/工具（服务被删、工具被删后打开编辑）。
 * `existingTools` 提供时同时过滤 toolId；不提供时只按服务过滤。
 */
export function pruneScope(
  value: McpKeyScopeValue,
  isValidServer: (serverId: string) => boolean,
  isValidTool?: (serverId: string, toolId: string) => boolean
): McpKeyScopeValue {
  const scopes = value.scopes
    .filter((s) => isValidServer(s.serverId))
    .map<McpKeyScope>((s) => {
      if (s.toolIds === null || !isValidTool) return s
      return { ...s, toolIds: s.toolIds.filter((id) => isValidTool(s.serverId, id)) }
    })
  // 过滤后工具变 0 个的服务保留（由校验提示用户补选），不静默删除整条范围
  return { ...value, scopes }
}

/** 从列表行还原编辑值；缺字段时按旧行为（全部服务）处理。 */
export function fromRow(row: McpKeyRow | null | undefined): McpKeyScopeValue {
  if (!row) return DEFAULT_SCOPE
  const allServers = row.allServers ?? true
  return {
    allServers,
    scopes: allServers
      ? []
      : (row.scopes ?? [])
          .filter((s): s is { serverId: string; toolIds?: string[] | null } => Boolean(s.serverId))
          .map((s) => ({ serverId: s.serverId, toolIds: s.toolIds ?? null })),
  }
}

/** 提交前转成请求体（保证 allServers 时 scopes 为空）。 */
export function toPayload(name: string, value: McpKeyScopeValue) {
  const normalized = normalizeScope(value)
  return {
    name: name.trim(),
    allServers: normalized.allServers,
    scopes: normalized.scopes,
  }
}
