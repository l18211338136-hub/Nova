import { useTranslation } from 'react-i18next'
import { Badge } from '@/components/ui/badge'
import { fromRow, summarizeScope } from '../lib/key-scope'
import type { McpKeyRow } from '../types'

/**
 * 列表「授权范围」列：全部服务 / N 个服务 · 全部工具 / N 个服务 · M 个工具。
 * 数据来自 `McpKeyDto.allServers` + `scopes`（后端上线后即生成模型字段）。
 */
export function McpKeyScopeCell({ row }: { row: McpKeyRow }) {
  const { t } = useTranslation()
  const summary = summarizeScope(fromRow(row))

  if (summary.kind === 'all') {
    return <Badge variant='secondary'>{t('All servers')}</Badge>
  }

  if (summary.kind === 'none') {
    return <span className='text-muted-foreground'>-</span>
  }

  return (
    <span className='text-xs text-nowrap'>
      {summary.allTools
        ? t('{{servers}} servers · all tools', { servers: summary.servers })
        : t('{{servers}} servers · {{tools}} tools', {
            servers: summary.servers,
            tools: summary.tools,
          })}
    </span>
  )
}
