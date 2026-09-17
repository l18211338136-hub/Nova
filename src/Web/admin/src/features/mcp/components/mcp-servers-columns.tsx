import { useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { type ColumnDef } from '@tanstack/react-table'
import { DataTableColumnHeader } from '@/components/data-table'
import { LongText } from '@/components/long-text'
import { type McpServerDto } from '../types'
import { McpRowActions } from './mcp-row-actions'

export const useMcpServersColumns = () => {
  const { t } = useTranslation()

  return useMemo<ColumnDef<McpServerDto>[]>(
    () => [
      {
        accessorKey: 'name',
        header: ({ column }) => (
          <DataTableColumnHeader column={column} title={t('Server Name')} />
        ),
        cell: ({ row }) => (
          <LongText className='max-w-40 ps-3 font-medium'>
            {row.getValue('name')}
          </LongText>
        ),
        meta: {
          title: t('Server Name'),
          filterType: 'text',
        },
      },
      {
        accessorKey: 'baseUrl',
        header: ({ column }) => (
          <DataTableColumnHeader column={column} title={t('Base URL')} />
        ),
        cell: ({ row }) => (
          <LongText className='max-w-56 ps-2 font-medium'>
            {row.getValue('baseUrl')}
          </LongText>
        ),
        meta: {
          title: t('Base URL'),
          filterType: 'text',
        },
      },
      {
        accessorKey: 'swaggerUrl',
        header: ({ column }) => (
          <DataTableColumnHeader column={column} title={t('Swagger URL')} />
        ),
        cell: ({ row }) => {
          const value = row.getValue('swaggerUrl') as string | null | undefined
          return (
            <LongText className='max-w-72 ps-2 text-muted-foreground break-all'>
              {value || '-'}
            </LongText>
          )
        },
        enableSorting: false,
        meta: {
          title: t('Swagger URL'),
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
      {
        id: 'actions',
        header: () => <div className='text-end'>{t('Actions')}</div>,
        cell: ({ row }) => (
          <div className='flex justify-end'>
            <McpRowActions row={row} kind='server' />
          </div>
        ),
        enableSorting: false,
        enableHiding: false,
        meta: {
          className: 'w-[60px]',
        },
      },
    ],
    [t]
  )
}
