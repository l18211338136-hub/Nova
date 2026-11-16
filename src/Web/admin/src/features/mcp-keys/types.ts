import type { McpKeyDto } from '@/api/model'

/**
 * 密钥的可访问范围项。
 * `toolIds === null` 表示该服务**全部工具**（含以后新增的工具）。
 */
export interface McpKeyScope {
  serverId: string
  toolIds: string[] | null
}

/** 弹窗里编辑的范围值（受控组件 `McpKeyScopeEditor` 的 value 形状）。 */
export interface McpKeyScopeValue {
  /** true = 全部服务（含未来新增）；此时 `scopes` 应为空 */
  allServers: boolean
  scopes: McpKeyScope[]
}

/**
 * 列表行（直接使用 gen-api 生成的 McpKeyDto，已含 allServers 与 scopes）。
 */
export type McpKeyRow = McpKeyDto

/** POST/PUT /api/mcp/keys 的请求体（目标契约）。 */
export interface McpKeyUpsertPayload {
  name: string
  allServers: boolean
  scopes: McpKeyScope[]
}

/** 默认范围：全部服务（与旧行为最接近，见 `01` Q-2）。 */
export const DEFAULT_SCOPE: McpKeyScopeValue = { allServers: true, scopes: [] }
