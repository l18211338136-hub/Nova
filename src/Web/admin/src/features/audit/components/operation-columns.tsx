import { useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { type ColumnDef } from '@tanstack/react-table'
import { ShieldAlert, Eye, Clock } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { DataTableColumnHeader } from '@/components/data-table'
import { type OperationLogDto } from '@/api/model'

interface UseOperationColumnsProps {
  onViewDetail: (log: OperationLogDto) => void
}

export const useOperationColumns = ({ onViewDetail }: UseOperationColumnsProps) => {
  const { t } = useTranslation()

  return useMemo<ColumnDef<OperationLogDto>[]>(
    () => [
      {
        accessorKey: 'httpMethod',
        header: ({ column }) => (
          <DataTableColumnHeader column={column} title={t('请求谓词 / 路径')} />
        ),
        cell: ({ row }) => {
          const method = (row.getValue('httpMethod') as string) || 'GET'
          const path = (row.original.requestPath as string) || '-'
          const actionName = row.original.actionName
          const isSlow = row.original.isSlowRequest

          const methodColorMap: Record<string, string> = {
            GET: 'bg-blue-100 text-blue-700 dark:bg-blue-950 dark:text-blue-300 border-blue-200',
            POST: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-950 dark:text-emerald-300 border-emerald-200',
            PUT: 'bg-amber-100 text-amber-700 dark:bg-amber-950 dark:text-amber-300 border-amber-200',
            DELETE: 'bg-rose-100 text-rose-700 dark:bg-rose-950 dark:text-rose-300 border-rose-200',
          }

          return (
            <div className='flex items-center gap-2 max-w-lg truncate ps-1'>
              <Badge variant='outline' className={cn('font-mono font-bold px-1.5 py-0.5 text-[11px] shrink-0', methodColorMap[method.toUpperCase()] || 'bg-muted')}>
                {method}
              </Badge>
              {actionName && (
                <Badge variant='secondary' className='font-semibold px-2 py-0.5 text-xs shrink-0 bg-primary/10 text-primary border border-primary/20'>
                  {actionName}
                </Badge>
              )}
              <span className='font-mono text-xs font-semibold text-muted-foreground truncate' title={path}>
                {path}
              </span>
              {isSlow && (
                <Badge variant='outline' className='bg-orange-100 text-orange-700 dark:bg-orange-950 dark:text-orange-300 gap-0.5 text-[10px] px-1 shrink-0'>
                  <Clock className='h-3 w-3' />
                  {t('慢日志')}
                </Badge>
              )}
            </div>
          )
        },
        meta: {
          filterType: 'select',
          title: t('请求谓词 / 路径'),
          className: 'w-[320px]',
          selectOptions: [
            { value: 'GET', label: 'GET' },
            { value: 'POST', label: 'POST' },
            { value: 'PUT', label: 'PUT' },
            { value: 'DELETE', label: 'DELETE' },
          ],
        },
      },
      {
        accessorKey: 'statusCode',
        header: ({ column }) => (
          <DataTableColumnHeader column={column} title={t('状态码 / 耗时')} />
        ),
        cell: ({ row }) => {
          const code = row.getValue('statusCode') as number | null
          const elapsedMs = row.original.elapsedMs

          const isSuccess = code && code >= 200 && code < 400

          return (
            <div className='flex items-center gap-2 ps-1'>
              <Badge
                variant='outline'
                className={cn(
                  'font-mono text-xs',
                  isSuccess
                    ? 'bg-green-100 text-green-700 dark:bg-green-950 dark:text-green-300'
                    : 'bg-red-100 text-red-700 dark:bg-red-950 dark:text-red-300'
                )}
              >
                {code ?? '-'}
              </Badge>
              <span className='text-xs text-muted-foreground font-mono'>
                {elapsedMs !== null && elapsedMs !== undefined ? `${elapsedMs}ms` : '-'}
              </span>
            </div>
          )
        },
        meta: {
          filterType: 'text',
          title: t('状态码 / 耗时'),
          className: 'w-[140px]',
          filterPlaceholder: t('状态码'),
        },
      },
      {
        accessorKey: 'hasSanitizedData',
        header: ({ column }) => (
          <DataTableColumnHeader column={column} title={t('自动脱敏')} />
        ),
        cell: ({ row }) => {
          const hasSanitized = row.getValue('hasSanitizedData') as boolean | null
          if (!hasSanitized) return <span className='text-muted-foreground text-xs ps-2'>-</span>

          return (
            <Badge variant='outline' className='bg-amber-100 text-amber-800 dark:bg-amber-950 dark:text-amber-300 gap-1 text-xs'>
              <ShieldAlert className='h-3.5 w-3.5' />
              {t('包含敏感词')}
            </Badge>
          )
        },
        meta: {
          filterType: 'boolean',
          title: t('自动脱敏'),
          className: 'w-[130px]',
          booleanOptions: {
            trueLabel: t('包含敏感词'),
            falseLabel: t('未脱敏'),
          },
        },
      },
      {
        accessorKey: 'clientIp',
        header: ({ column }) => (
          <DataTableColumnHeader column={column} title={t('客户端 IP')} />
        ),
        cell: ({ row }) => (
          <div className='font-mono text-xs ps-2 text-nowrap'>
            {row.getValue('clientIp') || '-'}
          </div>
        ),
        meta: {
          filterType: 'text',
          title: t('客户端 IP'),
          className: 'w-[140px]',
          filterPlaceholder: t('客户端 IP'),
        },
      },
      {
        accessorKey: 'createdAt',
        header: ({ column }) => (
          <DataTableColumnHeader column={column} title={t('时间')} />
        ),
        cell: ({ row }) => {
          const date = row.getValue('createdAt') as string
          return (
            <div className='ps-2 text-xs text-nowrap text-muted-foreground'>
              {date ? new Date(date).toLocaleString() : '-'}
            </div>
          )
        },
        meta: {
          filterType: 'date',
          title: t('时间'),
          className: 'w-[180px]',
        },
      },
      {
        id: 'actions',
        header: ({ column }) => (
          <DataTableColumnHeader column={column} title={t('操作')} />
        ),
        cell: ({ row }) => (
          <Button
            variant='ghost'
            size='sm'
            onClick={() => onViewDetail(row.original)}
            className='h-8 px-2 text-xs gap-1 text-primary hover:text-primary'
          >
            <Eye className='h-3.5 w-3.5' />
            {t('查看详情')}
          </Button>
        ),
        meta: {
          className: 'w-[100px]',
        },
      },
    ],
    [t, onViewDetail]
  )
}
