import { useMutation } from '@tanstack/react-query'
import { customInstance } from '@/lib/api-client'
import type { ApiResponse } from '@/features/mcp/types'
import type { McpKeyUpsertPayload } from '../types'

/**
 * 密钥创建 / 更新（目标契约：带 `allServers` + `scopes`）。
 *
 * ⚠️ 生成的 `useCreateMcpKey` / `useUpdateMcpKey` 只接受 `{ name }`，
 * 后端尚未扩展（见 `03-改动清单与接口契约` §3 B-04/B-05），因此这里按目标契约
 * 直接调用 `customInstance`，**不改生成物**。
 *
 * 后端上线后：
 *   1. `pnpm gen-api`
 *   2. 删除本文件，create/edit 弹窗改用生成的 `useCreateMcpKey` / `useUpdateMcpKey`
 */
export interface CreateMcpKeyResult {
  id: string
  keyValue: string
}

export function createMcpKeyWithScope(data: McpKeyUpsertPayload) {
  return customInstance<ApiResponse<CreateMcpKeyResult>>({
    url: '/api/mcp/keys',
    method: 'POST',
    data,
  })
}

export function updateMcpKeyWithScope(id: string, data: McpKeyUpsertPayload) {
  return customInstance<ApiResponse<null>>({
    url: `/api/mcp/keys/${id}`,
    method: 'PUT',
    data,
  })
}

export function useCreateMcpKeyWithScope() {
  return useMutation({
    mutationFn: (data: McpKeyUpsertPayload) => createMcpKeyWithScope(data),
  })
}

export function useUpdateMcpKeyWithScope() {
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: McpKeyUpsertPayload }) =>
      updateMcpKeyWithScope(id, data),
  })
}
