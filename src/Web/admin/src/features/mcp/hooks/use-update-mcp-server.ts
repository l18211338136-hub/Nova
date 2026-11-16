import { useMutation } from '@tanstack/react-query'
import { customInstance } from '@/lib/api-client'
import type { ApiResponse, McpServerDto } from '../types'

/**
 * 更新 MCP 服务（目标契约：`PUT /api/mcp/servers/{id}`）。
 *
 * ⚠️ 后端尚未提供该端点（`Servers/Commands/` 目前只有 Delete），因此这里
 * 直接按《03-改动清单与接口契约》§4 的目标契约手写调用，暂不写在
 * `src/api/endpoints/mcp.ts`（生成物，禁止手改）。
 *
 * 后端端点上线后：
 *   1. 在 `API_BASE_URL` 指向含新端点的后端上执行 `pnpm gen-api`
 *   2. 用生成的 `useUpdateMcpServer` 替换此文件，业务层无需改动
 */
export interface UpdateMcpServerPayload {
  name: string
  baseUrl: string
  swaggerUrl?: string | null
  /** 留空表示不修改：后端应沿用原值，不得以 null 覆盖 */
  authToken?: string | null
}

export function updateMcpServer(id: string, data: UpdateMcpServerPayload) {
  return customInstance<ApiResponse<McpServerDto>>({
    url: `/api/mcp/servers/${id}`,
    method: 'PUT',
    data,
  })
}

export function useUpdateMcpServer() {
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateMcpServerPayload }) =>
      updateMcpServer(id, data),
  })
}
