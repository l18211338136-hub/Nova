import { useMutation } from '@tanstack/react-query'
import { parse as parseSwagger } from '@/api/endpoints/mcp'
import type { ApiResponse } from '../types'

/** POST /api/mcp/parse 请求体（Swagger 地址 / JSON 二选一）。 */
export interface ParseSwaggerPayload {
  swaggerUrl?: string | null
  swaggerJson?: string | null
  authToken?: string | null
}

/** 后端解析出的单个接口操作。 */
export interface ParsedSwaggerOperationDto {
  method: string
  path: string
  summary: string
  group: string
}

/** POST /api/mcp/parse 响应数据：最终 Swagger JSON + 全部接口操作。 */
export interface ParseSwaggerResult {
  swaggerJson: string
  operations: ParsedSwaggerOperationDto[]
}

// 生成的 `parse` 被推断为 `Promise<void>`（后端返回 IActionResult 且未标注
// `[Produces<...>]`），与 servers/tools 一致，这里按 use-mcp-servers 的方式
// 将运行时 JSON 断言为 ApiResponse<ParseSwaggerResult> 信封。
export function useParseSwagger() {
  return useMutation({
    mutationFn: async (payload: ParseSwaggerPayload) => {
      const res = (await parseSwagger(
        payload as never
      )) as unknown as ApiResponse<ParseSwaggerResult>
      return res.data
    },
  })
}
