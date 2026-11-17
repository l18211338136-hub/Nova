'use client'

import { useEffect, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { AlertCircle, SearchX } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Skeleton } from '@/components/ui/skeleton'
import { TOOL_AUTO_EXPAND_LIMIT } from '../constants'
import { useServerToolsPaged } from '../hooks/use-server-tools'
import { shouldAutoExpand } from '../lib/tool-paging'
import type { McpServerDto } from '../types'
import { McpToolGroups } from './mcp-tool-groups'

type McpServerDetailDialogProps = {
  currentRow?: McpServerDto
  open: boolean
  onOpenChange: (open: boolean) => void
}

/**
 * MCP 服务详情：只读查看该服务的工具清单，按分组 / 分项展示。
 *
 * 数据来源：`GET /api/mcp/tools`（OData），统一走 `useServerToolsPaged`：
 * - ≤1000 一次加载完，分组/计数/搜索全部准确；
 * - >1000 首屏 1000 + 「加载更多」继续浏览（不是只能搜）；
 * - 搜索下推服务端，覆盖全部工具而非仅已加载部分。
 *
 * 分组规则见 `lib/mcp-tool-groups.ts`（与后端 DeriveGroup 对齐）。
 */
export function McpServerDetailDialog({
  currentRow,
  open,
  onOpenChange,
}: McpServerDetailDialogProps) {
  const { t } = useTranslation()

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className='sm:max-w-3xl'>
        <DialogHeader className='text-start'>
          <DialogTitle>
            {t('MCP Tools of {{name}}', { name: currentRow?.name ?? '' })}
          </DialogTitle>
          <DialogDescription className='break-all'>
            {currentRow?.baseUrl}
          </DialogDescription>
        </DialogHeader>
        {/* 取数放在 Body 内：Dialog 关闭即卸载，天然避免无谓请求 */}
        {currentRow?.id && <DetailBody serverId={currentRow.id} />}
      </DialogContent>
    </Dialog>
  )
}

function DetailBody({ serverId }: { serverId: string }) {
  const { t } = useTranslation()
  const [input, setInput] = useState('')
  const [search, setSearch] = useState('')
  // null = 用户尚未手动操作，按数据量自动决定默认展开/折叠
  const [collapsed, setCollapsed] = useState<Set<string> | null>(null)

  // 300ms 去抖：搜索走服务端，避免每个按键一次请求
  useEffect(() => {
    const timer = window.setTimeout(() => setSearch(input.trim()), 300)
    return () => window.clearTimeout(timer)
  }, [input])

  const {
    tools,
    groups,
    total,
    loaded,
    remaining,
    hasMore,
    isSearching,
    isLoading,
    isError,
    isLoadingMore,
    loadMore,
    refetch,
  } = useServerToolsPaged({ serverId, search })

  // 默认展开策略：加载量超过阈值时只显示组头，避免首屏渲染上千行。
  // 用惰性派生而不是 effect 里 setState——后者会触发级联渲染（lint 也会拦）。
  const defaultCollapsed = useMemo(() => {
    if (shouldAutoExpand(tools.length, TOOL_AUTO_EXPAND_LIMIT)) return new Set<string>()
    return new Set(groups.map((g) => g.name))
  }, [tools.length, groups])

  const collapsedSet = collapsed ?? defaultCollapsed

  const toggleGroup = (name: string) => {
    setCollapsed((prev) => {
      const next = new Set(prev ?? defaultCollapsed)
      if (next.has(name)) next.delete(name)
      else next.add(name)
      return next
    })
  }

  if (isLoading) {
    return (
      <div className='flex h-[60vh] flex-col gap-2'>
        {Array.from({ length: 8 }).map((_, i) => (
          <Skeleton key={i} className='h-9 w-full' />
        ))}
      </div>
    )
  }

  if (isError) {
    return (
      <div className='flex h-[60vh] flex-col items-center justify-center gap-3 text-center'>
        <AlertCircle className='size-6 text-destructive' />
        <p className='text-sm text-muted-foreground'>
          {t('Failed to load MCP tools.')}
        </p>
        <Button variant='outline' size='sm' onClick={refetch}>
          {t('Retry')}
        </Button>
      </div>
    )
  }

  if (tools.length === 0 && !isSearching) {
    return (
      <div className='flex h-[60vh] flex-col items-center justify-center gap-2 text-center'>
        <SearchX className='size-6 text-muted-foreground' />
        <p className='text-sm text-muted-foreground'>
          {t('No tools found for this server.')}
        </p>
      </div>
    )
  }

  return (
    <div className='flex h-[60vh] w-full min-w-0 flex-col gap-2'>
      {/* 窄屏（<400px）时按钮换行：输入框给 basis，避免把「展开/收起」挤出弹窗 */}
      <div className='flex w-full min-w-0 flex-wrap items-center gap-2'>
        <Input
          value={input}
          onChange={(e) => setInput(e.target.value)}
          placeholder={t('Search tools...')}
          className='h-8 min-w-0 flex-1 basis-40'
        />
        <Button
          type='button'
          variant='ghost'
          size='sm'
          className='h-8 px-2 text-xs'
          onClick={() => setCollapsed(new Set())}
        >
          {t('Expand all')}
        </Button>
        <Button
          type='button'
          variant='ghost'
          size='sm'
          className='h-8 px-2 text-xs'
          onClick={() => setCollapsed(new Set(groups.map((g) => g.name)))}
        >
          {t('Collapse all')}
        </Button>
      </div>

      <div className='flex flex-wrap items-center justify-between gap-x-3 gap-y-1 text-xs text-muted-foreground'>
        <span>
          {isSearching
            ? t('{{count}} matching tools', { count: total })
            : t('{{groups}} groups · loaded {{loaded}} of {{total}}', {
                groups: groups.length,
                loaded,
                total,
              })}
        </span>
        {hasMore && (
          <span className='text-amber-600 dark:text-amber-400'>
            {t('Only loaded tools are counted per group')}
          </span>
        )}
      </div>

      {groups.length === 0 ? (
        <div className='flex min-h-0 flex-1 items-center justify-center rounded-md border text-sm text-muted-foreground'>
          {t('No results found.')}
        </div>
      ) : (
        <McpToolGroups
          groups={groups}
          collapsed={collapsedSet}
          onToggleGroup={toggleGroup}
          forceExpanded={isSearching}
        />
      )}

      {hasMore && (
        <div className='flex shrink-0 items-center justify-center gap-2 pt-0.5'>
          <Button
            type='button'
            variant='outline'
            size='sm'
            className='h-8 text-xs'
            onClick={loadMore}
            disabled={isLoadingMore}
          >
            {isLoadingMore
              ? t('Loading more...')
              : t('Load more ({{count}} remaining)', { count: remaining })}
          </Button>
        </div>
      )}
    </div>
  )
}
