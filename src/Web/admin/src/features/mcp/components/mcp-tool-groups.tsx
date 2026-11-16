'use client'

import { useTranslation } from 'react-i18next'
import { ChevronDown } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { cn } from '@/lib/utils'
import { METHOD_FALLBACK_STYLE, METHOD_STYLES } from '../constants'
import type { McpToolGroup } from '../lib/mcp-tool-groups'

type McpToolGroupsProps = {
  groups: McpToolGroup[]
  /** 已折叠的分组名集合（受控） */
  collapsed: Set<string>
  onToggleGroup: (name: string) => void
  /** 搜索时强制展开，避免命中项被折叠隐藏 */
  forceExpanded?: boolean
}

/**
 * 工具列表的「分组 / 分项」只读渲染。
 *
 * 结构照搬 `mcp-import-dialog.tsx` 第 531-608 行（导入第 2 步的分组渲染），
 * 差异：去掉选择用 Checkbox，改为展示工具名 / 描述 / 启用 / 公开状态。
 */
export function McpToolGroups({
  groups,
  collapsed,
  onToggleGroup,
  forceExpanded = false,
}: McpToolGroupsProps) {
  const { t } = useTranslation()

  return (
    <div className='min-h-0 flex-1 overflow-y-auto overscroll-contain rounded-md border'>
      {groups.map((group) => {
        const expanded = forceExpanded || !collapsed.has(group.name)
        return (
          <div key={group.name} data-testid={`mcp-tool-group-${group.name}`}>
            {/* 组头：粘性 + 折叠；纯色背景（backdrop-blur 在 transform 容器内会触发 Chrome 合成层错位） */}
            <button
              type='button'
              onClick={() => onToggleGroup(group.name)}
              className='sticky top-0 z-10 flex w-full items-center gap-1.5 border-b bg-muted px-2 py-1.5 text-start'
            >
              <ChevronDown
                className={cn(
                  'size-3.5 shrink-0 text-muted-foreground transition-transform',
                  !expanded && '-rotate-90'
                )}
              />
              <span className='truncate text-xs font-medium'>{group.name}</span>
              <span className='shrink-0 rounded-full bg-background px-1.5 py-0.5 text-[10px] tabular-nums text-muted-foreground'>
                {group.tools.length}
              </span>
            </button>

            {expanded && (
              <div className='pb-1'>
                {group.tools.map((tool) => (
                  <div
                    key={tool.id}
                    className='flex items-center gap-2 rounded px-2 py-1.5 ps-8 hover:bg-muted/50'
                  >
                    <span
                      className={cn(
                        'inline-block w-12 shrink-0 rounded px-1 py-0.5 text-center font-mono text-[10px] font-medium leading-4',
                        METHOD_STYLES[tool.httpMethod] ?? METHOD_FALLBACK_STYLE
                      )}
                    >
                      {tool.httpMethod}
                    </span>
                    <div className='min-w-0 flex-1'>
                      <div className='truncate font-mono text-xs'>
                        {tool.routePath}
                      </div>
                      <div className='truncate text-[11px] text-muted-foreground'>
                        {tool.name}
                        {tool.description ? ` · ${tool.description}` : ''}
                      </div>
                    </div>
                    <Badge
                      variant={
                        tool.isEnabled ? 'secondary' : 'outline'
                      }
                      className={cn(
                        'shrink-0 text-[10px]',
                        tool.isEnabled
                          ? 'text-emerald-600 dark:text-emerald-400'
                          : 'text-muted-foreground'
                      )}
                    >
                      {tool.isEnabled ? t('Enabled') : t('Disabled')}
                    </Badge>
                    <Badge
                      variant='outline'
                      className='hidden shrink-0 text-[10px] text-muted-foreground sm:inline-flex'
                    >
                      {tool.isPublic ? t('Public') : t('Private')}
                    </Badge>
                  </div>
                ))}
              </div>
            )}
          </div>
        )
      })}
    </div>
  )
}
