'use client'

import { useEffect, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Search, X } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Checkbox } from '@/components/ui/checkbox'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group'
import { ScrollArea } from '@/components/ui/scroll-area'
import { METHOD_FALLBACK_STYLE, METHOD_STYLES } from '@/features/mcp/constants'
import { useMcpServers } from '@/features/mcp/hooks/use-mcp-servers'
import { useServerToolsPaged } from '@/features/mcp/hooks/use-server-tools'
import type { McpServerDto } from '@/features/mcp/types'
import { cn } from '@/lib/utils'
import {
  setServerAllTools,
  setServerTools,
  summarizeScope,
  toggleServer,
  toggleTool,
} from '../lib/key-scope'
import type { McpKeyScopeValue } from '../types'

type Props = {
  value: McpKeyScopeValue
  onChange: (value: McpKeyScopeValue) => void
}

/**
 * 密钥授权范围编辑器（受控）。
 *
 * 结构：① 全部服务 / 指定服务 ② 指定时勾选服务 ③ 为每个已选服务定工具范围。
 * 工具列表按 `deriveToolGroup` 分组（与服务「详情」同一套推导规则），
 * 每个已选服务单独取数，未选中的服务不请求。
 */
export function McpKeyScopeEditor({ value, onChange }: Props) {
  const { t } = useTranslation()
  const [search, setSearch] = useState('')

  const { data, isLoading } = useMcpServers({ $top: 200, $orderby: 'Name asc' })
  const servers = useMemo(() => (data?.data?.items ?? []) as McpServerDto[], [data])

  const selectedIds = useMemo(
    () => new Set(value.scopes.map((s) => s.serverId)),
    [value.scopes]
  )

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return servers
    return servers.filter(
      (s) =>
        s.name.toLowerCase().includes(q) ||
        (s.baseUrl ?? '').toLowerCase().includes(q)
    )
  }, [servers, search])

  const summary = summarizeScope(value)

  const setAll = (all: boolean) => {
    onChange(all ? { allServers: true, scopes: [] } : { allServers: false, scopes: value.scopes })
  }

  return (
    <div className='space-y-3'>
      <RadioGroup
        value={value.allServers ? 'all' : 'selected'}
        onValueChange={(v) => setAll(v === 'all')}
        className='gap-2'
      >
        <div className='flex items-start gap-2'>
          <RadioGroupItem value='all' id='scope-all' className='mt-0.5' />
          <Label htmlFor='scope-all' className='font-normal leading-tight'>
            <span className='font-medium'>{t('All servers')}</span>
            <span className='block text-xs text-muted-foreground'>
              {t('All servers hint')}
            </span>
          </Label>
        </div>
        <div className='flex items-start gap-2'>
          <RadioGroupItem value='selected' id='scope-selected' className='mt-0.5' />
          <Label htmlFor='scope-selected' className='font-normal leading-tight'>
            <span className='font-medium'>{t('Selected servers')}</span>
            <span className='block text-xs text-muted-foreground'>
              {t('Only the chosen servers and tools can be called by this key.')}
            </span>
          </Label>
        </div>
      </RadioGroup>

      {!value.allServers && (
        <div className='space-y-3 rounded-md border p-3'>
          {/* ① 选服务 */}
          <div className='relative'>
            <Search className='pointer-events-none absolute start-2 top-1/2 size-3.5 -translate-y-1/2 text-muted-foreground' />
            <Input
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder={t('Search servers...')}
              className='h-8 ps-7'
            />
          </div>

          <ScrollArea className='h-40 rounded-md border'>
            <div className='space-y-0.5 p-1'>
              {isLoading && (
                <p className='px-2 py-3 text-xs text-muted-foreground'>
                  {t('Loading...')}
                </p>
              )}
              {!isLoading && filtered.length === 0 && (
                <p className='px-2 py-3 text-xs text-muted-foreground'>
                  {t('No results.')}
                </p>
              )}
              {filtered.map((server) => (
                <label
                  key={server.id}
                  data-testid='key-server-option'
                  data-server-id={server.id}
                  className='flex cursor-pointer items-center gap-2 rounded-sm px-2 py-1.5 text-sm hover:bg-accent'
                >
                  <Checkbox
                    checked={selectedIds.has(server.id)}
                    onCheckedChange={(v) =>
                      onChange(toggleServer(value, server.id, Boolean(v)))
                    }
                  />
                  <span className='truncate'>{server.name}</span>
                  <span className='ms-auto hidden max-w-40 truncate text-xs text-muted-foreground sm:block'>
                    {server.baseUrl}
                  </span>
                </label>
              ))}
            </div>
          </ScrollArea>

          {/* ② 每个已选服务定工具范围 */}
          {value.scopes.length > 0 && (
            <div className='space-y-2'>
              <p className='text-xs font-medium text-muted-foreground'>
                {t('Tool scope per server')}
              </p>
              {value.scopes.map((scope) => {
                const server = servers.find((s) => s.id === scope.serverId)
                return (
                  <ServerScopeCard
                    key={scope.serverId}
                    serverId={scope.serverId}
                    serverName={server?.name ?? t('Server no longer exists')}
                    missing={!server}
                    toolIds={scope.toolIds}
                    onAllTools={(all, allIds) =>
                      onChange(setServerAllTools(value, scope.serverId, all, allIds))
                    }
                    onTools={(ids) => onChange(setServerTools(value, scope.serverId, ids))}
                    onToggleTool={(toolId) => onChange(toggleTool(value, scope.serverId, toolId))}
                    onRemove={() => onChange(toggleServer(value, scope.serverId, false))}
                  />
                )
              })}
            </div>
          )}

          <p data-testid='scope-summary' className='text-xs text-muted-foreground'>
            {summary.kind === 'all' && t('All servers')}
            {summary.kind === 'servers' &&
              (summary.allTools
                ? t('{{servers}} servers · all tools', { servers: summary.servers })
                : t('{{servers}} servers · {{tools}} tools', {
                    servers: summary.servers,
                    tools: summary.tools,
                  }))}
            {summary.kind === 'none' && t('No server selected')}
          </p>
        </div>
      )}
    </div>
  )
}

type CardProps = {
  serverId: string
  serverName: string
  missing: boolean
  toolIds: string[] | null
  onAllTools: (all: boolean, allToolIds: string[]) => void
  onTools: (ids: string[]) => void
  onToggleTool: (toolId: string) => void
  onRemove: () => void
}

function ServerScopeCard({
  serverId,
  serverName,
  missing,
  toolIds,
  onAllTools,
  onTools,
  onToggleTool,
  onRemove,
}: CardProps) {
  const { t } = useTranslation()
  const [expanded, setExpanded] = useState(toolIds !== null)
  const [input, setInput] = useState('')
  const [search, setSearch] = useState('')

  // 300ms 去抖：卡片内搜索同样下推服务端（>1000 时不能只过滤已加载集合）
  useEffect(() => {
    const timer = window.setTimeout(() => setSearch(input.trim()), 300)
    return () => window.clearTimeout(timer)
  }, [input])

  // 已选中的服务才请求工具；分页/搜索策略与「服务详情」共用同一个 hook
  const {
    tools,
    groups,
    total,
    serviceTotal,
    remaining,
    hasMore,
    isLoading,
    isLoadingMore,
    loadMore,
    isSearching,
  } = useServerToolsPaged({ serverId, search })

  const allIds = useMemo(() => tools.map((tool) => tool.id), [tools])

  const isAllTools = toolIds === null
  // 🔺 总数必须用接口的 total：allIds.length 只是已加载数，分页后会少报
  const selectedCount = isAllTools ? serviceTotal : (toolIds ?? []).length
  const isEmpty = !isAllTools && selectedCount === 0
  const selectedSet = useMemo(
    () => new Set(isAllTools ? allIds : (toolIds ?? [])),
    [isAllTools, toolIds, allIds]
  )

  return (
    <div data-testid='key-scope-card' data-server-id={serverId} className='rounded-md border bg-muted/30'>
      <div className='flex flex-wrap items-center gap-2 px-2 py-1.5'>
        <span className='min-w-0 flex-1 truncate text-sm font-medium'>{serverName}</span>
        {missing ? (
          <Badge variant='outline'>{t('Server no longer exists')}</Badge>
        ) : (
          <span className='text-xs text-muted-foreground'>
            {isLoading
              ? t('Loading...')
              : t('{{selected}} / {{total}} tools', { selected: selectedCount, total: serviceTotal })}
          </span>
        )}
        <div className='flex items-center gap-1'>
          <Button
            type='button'
            size='sm'
            variant={isAllTools ? 'default' : 'outline'}
            className='h-6 px-2 text-xs'
            disabled={missing}
            onClick={() => onAllTools(true, allIds)}
          >
            {t('All tools')}
          </Button>
          <Button
            type='button'
            size='sm'
            variant={isAllTools ? 'outline' : 'default'}
            className='h-6 px-2 text-xs'
            disabled={missing}
            onClick={() => {
              setExpanded(true)
              onAllTools(false, allIds)
            }}
          >
            {t('Selected tools')}
          </Button>
          <Button
            type='button'
            size='icon'
            variant='ghost'
            className='size-6'
            aria-label={t('Remove')}
            onClick={onRemove}
          >
            <X className='size-3.5' />
          </Button>
        </div>
      </div>

      {!isAllTools && hasMore && (
        <p className='px-2 pb-1 text-[11px] text-amber-600 dark:text-amber-400'>
          {t('Only loaded tools are counted per group')}
        </p>
      )}

      {!isAllTools && expanded && (
        <div className='border-t px-2 py-2'>
          <div className='mb-2 flex items-center gap-2'>
            <Input
              value={input}
              onChange={(e) => setInput(e.target.value)}
              placeholder={t('Search tools...')}
              className='h-7 flex-1 text-xs'
            />
            {isSearching && !isLoading && (
              <span className='shrink-0 text-[11px] text-muted-foreground'>
                {t('{{count}} matching tools', { count: total })}
              </span>
            )}
          </div>
          {isLoading ? (
            <p className='py-2 text-xs text-muted-foreground'>{t('Loading...')}</p>
          ) : tools.length === 0 ? (
            <p className='py-2 text-xs text-muted-foreground'>
              {t('No tools found for this server.')}
            </p>
          ) : (
            <div className='max-h-56 space-y-2 overflow-y-auto'>
              {groups.map((group) => {
                const groupIds = group.tools.map((tool) => tool.id)
                const checkedCount = groupIds.filter((id) => selectedSet.has(id)).length
                const allChecked = checkedCount === groupIds.length
                return (
                  <div key={group.name} data-testid='key-tool-group' className='rounded border bg-background/60'>
                    <label className='flex cursor-pointer items-center gap-2 px-2 py-1 text-xs'>
                      <Checkbox
                        checked={allChecked ? true : checkedCount > 0 ? 'indeterminate' : false}
                        onCheckedChange={(v) => {
                          const next = new Set(selectedSet)
                          groupIds.forEach((id) => (v ? next.add(id) : next.delete(id)))
                          onTools([...next])
                        }}
                      />
                      <span className='font-medium'>{group.name}</span>
                      <span className='text-muted-foreground'>
                        {checkedCount}/{groupIds.length}
                      </span>
                      <button
                        type='button'
                        className='ms-auto text-[11px] text-muted-foreground underline-offset-2 hover:underline'
                        onClick={(e) => {
                          e.preventDefault()
                          const next = new Set(selectedSet)
                          groupIds.forEach((id) => next.add(id))
                          onTools([...next])
                        }}
                      >
                        {t('Select all')}
                      </button>
                    </label>
                    <div className='space-y-0.5 px-2 pb-1.5'>
                      {group.tools.map((tool) => (
                        <label
                          key={tool.id}
                          className='flex cursor-pointer items-center gap-2 rounded-sm px-1 py-1 text-xs hover:bg-accent'
                        >
                          <Checkbox
                            checked={selectedSet.has(tool.id)}
                            onCheckedChange={() => onToggleTool(tool.id)}
                          />
                          <span
                            className={cn(
                              'w-12 shrink-0 rounded px-1 text-center font-mono text-[10px] font-medium',
                              METHOD_STYLES[tool.httpMethod] ?? METHOD_FALLBACK_STYLE
                            )}
                          >
                            {tool.httpMethod}
                          </span>
                          <span className='truncate font-mono text-[11px] text-muted-foreground'>
                            {tool.routePath}
                          </span>
                          <span className='truncate'>{tool.name}</span>
                        </label>
                      ))}
                    </div>
                  </div>
                )
              })}
            </div>
          )}
          {hasMore && (
            <div className='mt-2 flex justify-center'>
              <Button
                type='button'
                size='sm'
                variant='outline'
                className='h-7 text-xs'
                onClick={loadMore}
                disabled={isLoadingMore}
              >
                {isLoadingMore
                  ? t('Loading more...')
                  : t('Load more ({{count}} remaining)', { count: remaining })}
              </Button>
            </div>
          )}
          {isEmpty && (
            <p className='mt-1 text-xs text-destructive'>{t('Select at least one tool.')}</p>
          )}
        </div>
      )}
    </div>
  )
}
