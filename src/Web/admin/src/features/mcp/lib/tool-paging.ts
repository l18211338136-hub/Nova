import type { McpToolDto } from '../types'

/**
 * 工具列表取数的纯函数：过滤表达式、分页合并、已加载摘要。
 *
 * 抽成纯函数的原因：这些逻辑同时被「服务详情」与「密钥授权范围」使用，
 * 写在组件里必然漂移（曾出现两处 `$top` 各写一份、一处 500 一处全量）。
 */

/**
 * 组装 OData `$filter`。
 *
 * - 无搜索：`ServerId eq {id}`
 * - 有搜索：`ServerId eq {id} and (contains(Name,'q') or contains(RoutePath,'q'))`
 *
 * 🔺 单引号必须转义为 `''`，否则 `o'brien` 这类词会让 OData 解析失败（400）。
 */
export function buildToolFilter(serverId: string, search?: string): string {
  const base = `ServerId eq ${serverId}`
  const q = (search ?? '').trim()
  if (!q) return base
  const escaped = q.replace(/'/g, "''")
  return `${base} and (contains(Name,'${escaped}') or contains(RoutePath,'${escaped}'))`
}

/**
 * 合并多页工具，按 `id` 去重（保留首次出现顺序）。
 *
 * 为什么需要去重：翻页期间若有工具新增/删除，`$skip` 会错位，同一 id 可能出现在两页。
 */
export function mergeToolPages(pages: McpToolDto[][]): McpToolDto[] {
  const seen = new Set<string>()
  const merged: McpToolDto[] = []
  for (const page of pages) {
    for (const tool of page) {
      const id = tool.id ?? ''
      if (id && seen.has(id)) continue
      if (id) seen.add(id)
      merged.push(tool)
    }
  }
  return merged
}

export type LoadedSummary = {
  loaded: number
  total: number
  /** 还可加载的条数（不会为负） */
  remaining: number
  hasMore: boolean
}

/** 已加载 / 总数摘要；`total` 必须来自接口，不能用「已加载条数」代替。 */
export function summarizeLoaded(loaded: number, total: number): LoadedSummary {
  const safeLoaded = Math.max(loaded, 0)
  const safeTotal = Math.max(total, 0)
  const remaining = Math.max(safeTotal - safeLoaded, 0)
  return { loaded: safeLoaded, total: safeTotal, remaining, hasMore: remaining > 0 }
}

/** 加载量超过阈值时默认折叠分组，避免首屏渲染上千行 DOM。 */
export function shouldAutoExpand(loaded: number, limit: number): boolean {
  return loaded <= limit
}
