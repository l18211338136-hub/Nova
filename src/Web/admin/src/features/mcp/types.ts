// Local MCP domain types.
// NOTE: The generated Orval client (`@/api/endpoints/mcp.ts`) currently infers the
// `servers` / `tools` endpoints as `void` because the backend `McpController`
// returns `IActionResult` without `[Produces<...>]`. We keep the canonical shapes
// here so the page is fully typed. Once the backend adds `[Produces<ApiResponse<PagedResult<McpServerDto>>>(200)]`
// and the client is regenerated (`pnpm gen-api`), these can be dropped in favor of
// the generated `McpServerDto` / `McpToolDto` models.

export interface McpServerDto {
  id: string
  name: string
  baseUrl: string
  swaggerUrl?: string | null
  createdAt: string
}

export interface McpToolDto {
  id: string
  serverId: string
  name: string
  description: string
  isEnabled: boolean
  isPublic: boolean
  routePath: string
  httpMethod: string
  createdAt: string
}

export interface PagedResult<T> {
  total: number
  items: T[]
  page?: number | null
  pageSize?: number | null
}

export interface ApiResponse<T> {
  success: boolean
  message?: string
  data: T
}

/** POST /api/mcp/import 请求体（在生成的 ImportOpenApi 基础上扩展了 selectedOperations）。 */
export interface ImportOpenApiPayload {
  serverName: string
  baseUrl: string
  swaggerUrl?: string | null
  swaggerJson: string
  authToken?: string | null
  /** 仅导入选中的操作，格式 "METHOD 路径"（如 "GET /api/foo"）。为空时导入全部。 */
  selectedOperations?: string[] | null
}

/** 从 Swagger JSON 解析出的单个接口操作（前端选择列表用）。 */
export interface ParsedSwaggerOperation {
  /** 形如 "GET /api/foo" 的唯一键，与后端 SelectedOperations 格式一致。 */
  key: string
  method: string
  path: string
  summary: string
  /** 分组名：优先取 Swagger 的 tags[0]，否则取路径前缀（如 "/api/App"）。 */
  group: string
}
