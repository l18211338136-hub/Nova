import { useQuery } from '@tanstack/react-query'
import { servers as fetchServers, tools as fetchTools } from '@/api/endpoints/mcp'
import type { ApiResponse, PagedResult, McpServerDto, McpToolDto } from '../types'

// The generated `servers` / `tools` are typed as `Promise<void>` (backend returns
// `IActionResult` without `[Produces<...>]`), so we cast the runtime JSON to our
// local `ApiResponse<PagedResult<...>>` shape. The actual HTTP response is the
// `ApiResponse<T>` envelope produced by `ApiResponse<...>.Success(...)`.
export function useMcpServers(params: Record<string, unknown>) {
  return useQuery<ApiResponse<PagedResult<McpServerDto>>>({
    queryKey: ['mcp-servers', params],
    queryFn: async () =>
      (await fetchServers({ params })) as unknown as ApiResponse<
        PagedResult<McpServerDto>
      >,
  })
}

export function useMcpTools(params: Record<string, unknown>) {
  return useQuery<ApiResponse<PagedResult<McpToolDto>>>({
    queryKey: ['mcp-tools', params],
    queryFn: async () =>
      (await fetchTools({ params })) as unknown as ApiResponse<
        PagedResult<McpToolDto>
      >,
  })
}
