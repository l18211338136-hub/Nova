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
 * 详情视图单次拉取工具数的上限。
 * 与 `05-测试报告` 中「上限提示」一致：超过时提示用搜索定位，避免一次渲染上千行。
 */
export const DETAIL_TOOL_LIMIT = 500
