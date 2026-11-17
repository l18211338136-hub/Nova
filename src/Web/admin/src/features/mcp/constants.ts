/**
 * MCP 模块共享常量。
 *
 * METHOD_STYLES 原定义在 `mcp-import-dialog.tsx`，详情视图需要同一套配色，
 * 提取到此处避免两处漂移（导入弹窗改为从这里引用，取值不变）。
 */

/** HTTP 方法 → Tailwind 色板（浅色/深色各一套）。未知方法回退 muted。 */
export const METHOD_STYLES: Record<string, string> = {
  GET: 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400',
  POST: 'bg-sky-500/10 text-sky-600 dark:text-sky-400',
  PUT: 'bg-amber-500/10 text-amber-600 dark:text-amber-400',
  DELETE: 'bg-red-500/10 text-red-600 dark:text-red-400',
  PATCH: 'bg-violet-500/10 text-violet-600 dark:text-violet-400',
}

/** method 未知时的兜底样式 */
export const METHOD_FALLBACK_STYLE = 'bg-muted text-muted-foreground'

/**
 * 工具列表取数策略（见 `20261117-feat-mcp工具列表取数改造` §2）。
 *
 * - `TOOL_PAGE_SIZE`：首屏与「加载更多」的页大小。≤该值的服务一次加载完，
 *   分组/计数/搜索全部准确；超过则分页续加载（用户可继续浏览，不只靠搜索）。
 * - `TOOL_SEARCH_PAGE_SIZE`：搜索态页大小（结果通常远小于全量）。
 * - `TOOL_AUTO_EXPAND_LIMIT`：超过该条数时默认折叠所有分组，避免首屏渲染上千行。
 *
 * 🔺 后端 `$top` 缺省 = 全量且无 MaxTop，前端必须显式传 `$top`，否则可能拉全库。
 */
export const TOOL_PAGE_SIZE = 1000
export const TOOL_SEARCH_PAGE_SIZE = 200
export const TOOL_AUTO_EXPAND_LIMIT = 200
