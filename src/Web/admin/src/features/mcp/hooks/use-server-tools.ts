import { useMemo } from 'react'
import { keepPreviousData, useInfiniteQuery, useQuery } from '@tanstack/react-query'
import { tools as fetchTools } from '@/api/endpoints/mcp'
import type { ApiResponse, McpToolDto, PagedResult } from '../types'
import { TOOL_PAGE_SIZE, TOOL_SEARCH_PAGE_SIZE } from '../constants'
import { groupTools } from '../lib/mcp-tool-groups'
import { buildToolFilter, mergeToolPages, summarizeLoaded } from '../lib/tool-paging'

type Args = {
  serverId: string
  /** 已去抖的搜索词，空串表示不过滤 */
  search?: string
  /** 每页条数；不传按「是否搜索」自动取常量 */
  pageSize?: number
  /** 未选中服务时可关闭请求 */
  enabled?: boolean
}

type Page = { items: McpToolDto[]; total: number }

/**
 * 服务工具列表的统一取数入口（详情弹窗 + 密钥授权范围共用）。
 *
 * 策略（`20261117-feat-mcp工具列表取数改造` §3）：
 * - 首屏请求 `$top=1000` 且带 `$count`：≤1000 一次拿全，分组/计数/搜索都准确；
 * - >1000 时 `hasMore=true`，由 UI 提供「加载更多」按页续取，用户可以继续浏览；
 * - 搜索下推到服务端（`contains(Name) or contains(RoutePath)`），覆盖全部工具，
 *   而不是只过滤已加载的那一页。
 *
 * 🔺 后端 `$top` 缺省 = 全量且无 MaxTop，这里始终显式传 `$top`。
 */
export function useServerToolsPaged({ serverId, search = '', pageSize, enabled = true }: Args) {
  const isSearching = search.trim().length > 0
  const size = pageSize ?? (isSearching ? TOOL_SEARCH_PAGE_SIZE : TOOL_PAGE_SIZE)
  const filter = buildToolFilter(serverId, search)

  const query = useInfiniteQuery({
    queryKey: ['mcp-server-tools', serverId, isSearching ? search.trim() : '', size, filter],
    enabled: enabled && Boolean(serverId),
    initialPageParam: 0,
    queryFn: async ({ pageParam }) => {
      // 生成物把该端点推断为 void（后端返回 IActionResult），运行时是 ApiResponse 信封
      const response = (await fetchTools({
        params: {
          $count: true,
          $filter: filter,
          $orderby: 'RoutePath asc',
          $skip: pageParam,
          $top: size,
        },
      })) as unknown as ApiResponse<PagedResult<McpToolDto>>

      return {
        items: (response?.data?.items as McpToolDto[]) ?? [],
        total: response?.data?.total ?? 0,
      } satisfies Page
    },
    // 下一页起点 = 已加载条数（OData $skip 从 0 起）
    getNextPageParam: (lastPage, allPages) => {
      const loaded = allPages.reduce((sum, page) => sum + page.items.length, 0)
      return loaded < lastPage.total ? loaded : undefined
    },
    // 切搜索词时保留旧结果，避免列表闪成空白
    placeholderData: keepPreviousData,
  })

  const tools = useMemo(
    () => mergeToolPages(query.data?.pages.map((page) => page.items) ?? []),
    [query.data]
  )
  const groups = useMemo(() => groupTools(tools), [tools])

  // 总数取最后一页的 total：搜索时它是匹配数，无搜索时是该服务的工具总数
  const pages = query.data?.pages ?? []
  const total = pages.length > 0 ? pages[pages.length - 1].total : 0
  // 搜索时 `total` 是匹配数，但「已选 x / 共 N」需要服务级总数，
  // 因此单独用 `$top=0&$count=true` 取一次服务级总数（与搜索词无关，按 serverId 缓存）。
  const countQuery = useQuery({
    queryKey: ['mcp-server-tool-count', serverId],
    enabled: enabled && Boolean(serverId),
    queryFn: async () => {
      const response = (await fetchTools({
        params: { $count: true, $top: 0, $filter: `ServerId eq ${serverId}` },
      })) as unknown as ApiResponse<PagedResult<McpToolDto>>
      return response?.data?.total ?? 0
    },
  })
  const serviceTotal = countQuery.data ?? total

  const summary = summarizeLoaded(tools.length, total)

  return {
    tools,
    groups,
    total,
    serviceTotal,
    loaded: summary.loaded,
    remaining: summary.remaining,
    hasMore: summary.hasMore,
    isSearching,
    isLoading: query.isLoading,
    isFetching: query.isFetching,
    isError: query.isError,
    isLoadingMore: query.isFetchingNextPage,
    loadMore: () => void query.fetchNextPage(),
    refetch: () => void query.refetch(),
  }
}
