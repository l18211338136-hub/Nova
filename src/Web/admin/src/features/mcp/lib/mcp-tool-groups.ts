import type { McpToolDto } from '../types'

export interface McpToolGroup {
  /** 分组名；后端未落库时为按 routePath 推导出的值，如 `/api/Identity` */
  name: string
  tools: McpToolDto[]
}

/**
 * 按 `routePath` 推导分组名。
 *
 * 🔺 规则必须与后端 `ParseSwaggerCommandConsumer.DeriveGroup`（第 170-183 行）
 * 逐字对齐，否则「导入时看到的分组」与「详情里看到的分组」会不一致：
 *   1. 有 Swagger tag 时分组 = tag（落库前由后端决定，前端拿不到）
 *   2. 无 tag：路径按 `/` 切段；首段为 `api` 且段数 > 1 → `/api/<第二段>`
 *   3. 否则 → `/<第一段>`
 *   4. 空路径 → `/`
 */
export function deriveGroupFromPath(routePath: string): string {
  const segments = (routePath ?? '').split('/').filter(Boolean)
  if (segments.length === 0) return '/'
  if (segments[0].toLowerCase() === 'api' && segments.length > 1) {
    return `/${segments[0]}/${segments[1]}`
  }
  return `/${segments[0]}`
}

/**
 * 取工具的所属分组。
 * 优先使用后端落库的 `group`（二期字段），缺省时由 `routePath` 推导。
 */
export function deriveToolGroup(tool: McpToolDto): string {
  const persisted = tool.group?.trim()
  if (persisted) return persisted
  return deriveGroupFromPath(tool.routePath)
}

/**
 * 工具列表 → 分组列表。
 * 分组按名称升序；组内按 `routePath` 升序（同路径再按 method），保证渲染稳定。
 */
export function groupTools(tools: McpToolDto[]): McpToolGroup[] {
  const map = new Map<string, McpToolDto[]>()
  for (const tool of tools) {
    const name = deriveToolGroup(tool)
    const bucket = map.get(name)
    if (bucket) bucket.push(tool)
    else map.set(name, [tool])
  }

  return [...map.entries()]
    .map(([name, groupToolsList]) => ({
      name,
      tools: [...groupToolsList].sort((a, b) => {
        const byPath = a.routePath.localeCompare(b.routePath)
        return byPath !== 0 ? byPath : a.httpMethod.localeCompare(b.httpMethod)
      }),
    }))
    .sort((a, b) => a.name.localeCompare(b.name))
}

/**
 * 按关键字过滤分组：命中 tool 的名称 / 路径 / 描述 / method 即保留，
 * 过滤后为空的整组丢弃（与导入弹窗的搜索行为一致）。
 */
export function filterToolGroups(
  groups: McpToolGroup[],
  query: string
): McpToolGroup[] {
  const q = query.trim().toLowerCase()
  if (!q) return groups

  return groups
    .map((group) => ({
      name: group.name,
      tools: group.tools.filter(
        (tool) =>
          tool.routePath.toLowerCase().includes(q) ||
          tool.name.toLowerCase().includes(q) ||
          tool.description?.toLowerCase().includes(q) ||
          tool.httpMethod.toLowerCase().includes(q) ||
          group.name.toLowerCase().includes(q)
      ),
    }))
    .filter((group) => group.tools.length > 0)
}
