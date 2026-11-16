'use client'

import { useMemo, useState } from 'react'
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
import { DETAIL_TOOL_LIMIT } from '../constants'
import { useMcpTools } from '../hooks/use-mcp-servers'
import { filterToolGroups, groupTools } from '../lib/mcp-tool-groups'
import type { McpServerDto, McpToolDto, PagedResult, ApiResponse } from '../types'
import { McpToolGroups } from './mcp-tool-groups'

type McpServerDetailDialogProps = {
  currentRow?: McpServerDto
  open: boolean
  onOpenChange: (open: boolean) => void
}

/**
 * MCP 服务详情：只读查看该服务的工具清单，按分组 / 分项展示。
 *
 * 数据来源：`GET /api/mcp/tools?$filter=ServerId eq {id}`（OData）。
 * 🔺 与列表页的 `$skip/$top/$orderby` 参数形状不同，**不共用参数对象**。
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
  const [search, setSearch] = useState('')
  const [collapsed, setCollapsed] = useState<Set<string>>(new Set())

  const { data, isLoading, isError, refetch } = useMcpTools({
    $count: true,
    $filter: `ServerId eq ${serverId}`,
    $top: DETAIL_TOOL_LIMIT,
    $orderby: 'RoutePath asc',
  })

  const response = data as unknown as
    | ApiResponse<PagedResult<McpToolDto>>
    | undefined
  const tools = useMemo(
    () => (response?.data?.items as McpToolDto[]) ?? [],
    [response]
  )
  const total = response?.data?.total ?? 0

  const groups = useMemo(() => groupTools(tools), [tools])
  const filteredGroups = useMemo(
    () => filterToolGroups(groups, search),
    [groups, search]
  )
  const isSearching = search.trim().length > 0
  const matchedCount = filteredGroups.reduce((n, g) => n + g.tools.length, 0)

  const toggleGroup = (name: string) => {
    setCollapsed((prev) => {
      const next = new Set(prev)
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
        <Button variant='outline' size='sm' onClick={() => refetch()}>
          {t('Retry')}
        </Button>
      </div>
    )
  }

  if (tools.length === 0) {
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
          value={search}
          onChange={(e) => setSearch(e.target.value)}
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

      <div className='flex items-center justify-between text-xs text-muted-foreground'>
        <span>
          {t('{{groups}} groups · {{tools}} tools', {
            groups: filteredGroups.length,
            tools: isSearching ? matchedCount : tools.length,
          })}
        </span>
        {total > tools.length && (
          <span className='text-amber-600 dark:text-amber-400'>
            {t('Only the first {{count}} tools are shown.', {
              count: tools.length,
            })}
          </span>
        )}
      </div>

      {filteredGroups.length === 0 ? (
        <div className='flex min-h-0 flex-1 items-center justify-center rounded-md border text-sm text-muted-foreground'>
          {t('No results found.')}
        </div>
      ) : (
        <McpToolGroups
          groups={filteredGroups}
          collapsed={collapsed}
          onToggleGroup={toggleGroup}
          forceExpanded={isSearching}
        />
      )}
    </div>
  )
}
