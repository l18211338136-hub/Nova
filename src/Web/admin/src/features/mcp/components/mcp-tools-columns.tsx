import { useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { type ColumnDef } from '@tanstack/react-table'
import { cn } from '@/lib/utils'
import { Badge } from '@/components/ui/badge'
import { DataTableColumnHeader } from '@/components/data-table'
import { LongText } from '@/components/long-text'
import { type McpToolDto } from '../types'

export const useMcpToolsColumns = () => {
  const { t } = useTranslation()

  return useMemo<ColumnDef<McpToolDto>[]>(
    () => [
      {
        accessorKey: 'name',
        header: ({ column }) => (
          <DataTableColumnHeader column={column} title={t('Tool Name')} />
        ),
        cell: ({ row }) => (
          <LongText className='max-w-40 ps-3 font-medium'>
            {row.getValue('name')}
          </LongText>
        ),
        meta: {
          title: t('Tool Name'),
          filterType: 'text',
        },
      },
      {
        accessorKey: 'description',
        header: ({ column }) => (
          <DataTableColumnHeader column={column} title={t('Description')} />
        ),
        cell: ({ row }) => {
          const value = row.getValue('description') as string
          return <LongText className='max-w-64 ps-2'>{value || '-'}</LongText>
        },
        enableSorting: false,
        meta: {
          title: t('Description'),
        },
      },
      {
        accessorKey: 'routePath',
        header: ({ column }) => (
          <DataTableColumnHeader column={column} title={t('Route Path')} />
        ),
        cell: ({ row }) => {
          const value = row.getValue('routePath') as string
          return (
            <code className='rounded bg-muted px-1.5 py-0.5 text-xs'>{value}</code>
          )
        },
        meta: {
          title: t('Route Path'),
        },
      },
      {
        accessorKey: 'httpMethod',
        header: ({ column }) => (
          <DataTableColumnHeader column={column} title={t('HTTP Method')} />
        ),
        cell: ({ row }) => {
          const method = row.getValue('httpMethod') as string
          return (
            <Badge variant='secondary' className='font-mono'>
              {method}
            </Badge>
          )
        },
        meta: {
          title: t('HTTP Method'),
        },
      },
      {
        accessorKey: 'isEnabled',
        header: ({ column }) => (
          <DataTableColumnHeader column={column} title={t('Enabled')} />
        ),
        cell: ({ row }) => {
          const enabled = row.getValue('isEnabled') as boolean
          return (
            <Badge
              variant='outline'
              className={cn(
                enabled
                  ? 'bg-green-100 text-green-700 dark:bg-green-900 dark:text-green-300'
                  : 'bg-red-100 text-red-700 dark:bg-red-900 dark:text-red-300'
              )}
            >
              {enabled ? t('Enabled') : t('Disabled')}
            </Badge>
          )
        },
        meta: {
          title: t('Enabled'),
          filterType: 'boolean',
        },
      },
      {
        accessorKey: 'isPublic',
        header: ({ column }) => (
          <DataTableColumnHeader column={column} title={t('Public')} />
        ),
        cell: ({ row }) => {
          const isPublic = row.getValue('isPublic') as boolean
          return (
            <Badge
              variant='outline'
              className={cn(
                isPublic
                  ? 'bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300'
                  : 'bg-muted text-muted-foreground'
              )}
            >
              {isPublic ? t('Public') : t('Private')}
            </Badge>
          )
        },
        meta: {
          title: t('Public'),
          filterType: 'boolean',
        },
      },
      {
        accessorKey: 'createdAt',
        header: ({ column }) => (
          <DataTableColumnHeader column={column} title={t('Created At')} />
        ),
        cell: ({ row }) => {
          const date = row.getValue('createdAt') as string
          return <div>{new Date(date).toLocaleString()}</div>
        },
        meta: {
          title: t('Created At'),
          filterType: 'date',
        },
      },
    ],
    [t]
  )
}
