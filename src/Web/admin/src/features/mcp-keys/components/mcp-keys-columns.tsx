import { useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { type ColumnDef } from '@tanstack/react-table'
import { cn } from '@/lib/utils'
import { Checkbox } from '@/components/ui/checkbox'
import { DataTableColumnHeader } from '@/components/data-table'
import { LongText } from '@/components/long-text'
import { type McpKeyDto as McpKey } from '@/api/model'
import { DataTableRowActions } from './mcp-keys-row-actions'
import { McpKeyCell } from './mcp-key-cell'

export const useMcpKeysColumns = () => {
  const { t } = useTranslation()

  return useMemo<ColumnDef<McpKey>[]>(() => [
    {
      id: 'select',
      header: ({ table }) => (
        <Checkbox
          checked={
            table.getIsAllPageRowsSelected() ||
            (table.getIsSomePageRowsSelected() && 'indeterminate')
          }
          onCheckedChange={(value) => table.toggleAllPageRowsSelected(!!value)}
          aria-label='Select all'
          className='translate-y-0.5'
        />
      ),
      meta: {
        className: cn('inset-s-0 z-10 rounded-tl-[inherit] w-[40px]'),
      },
      cell: ({ row }) => (
        <Checkbox
          checked={row.getIsSelected()}
          onCheckedChange={(value) => row.toggleSelected(!!value)}
          aria-label='Select row'
          className='translate-y-0.5'
        />
      ),
      enableSorting: false,
      enableHiding: false,
      enableColumnFilter: false,
    },
    {
      accessorKey: 'name',
      header: ({ column }) => (
        <DataTableColumnHeader column={column} title={t('Name')} />
      ),
      cell: ({ row }) => (
        <LongText className='max-w-48 ps-3'>{row.getValue('name')}</LongText>
      ),
      meta: {
        title: t('Name'),
        className: cn(
          'drop-shadow-[0_1px_2px_rgb(0_0_0_/_0.1)] dark:drop-shadow-[0_1px_2px_rgb(255_255_255_/_0.1)]',
          'inset-s-6 ps-0.5 max-md:sticky w-[200px]'
        ),
        filterType: 'text',
      },
      enableHiding: false,
    },
    {
      accessorKey: 'keyValue',
      header: ({ column }) => (
        <DataTableColumnHeader column={column} title={t('MCP Key')} />
      ),
      cell: ({ row }) => <McpKeyCell value={row.getValue('keyValue')} />,
      enableSorting: false,
      enableColumnFilter: false,
      meta: {
        title: t('MCP Key'),
        className: 'w-[420px]',
      },
    },
    {
      accessorKey: 'createdAt',
      header: ({ column }) => (
        <DataTableColumnHeader column={column} title={t('Created Date')} />
      ),
      cell: ({ row }) => {
        const date = row.getValue('createdAt') as string
        return (
          <div className='ps-2 text-nowrap'>
            {date ? new Date(date).toLocaleDateString() : '-'}
          </div>
        )
      },
      meta: {
        title: t('Created Date'),
        filterType: 'date',
        className: 'w-[160px]',
      },
    },
    {
      id: 'validity',
      header: ({ column }) => (
        <DataTableColumnHeader column={column} title={t('Validity')} />
      ),
      cell: () => <span className='ps-2 text-nowrap'>{t('Permanent')}</span>,
      enableSorting: false,
      enableColumnFilter: false,
      meta: {
        title: t('Validity'),
        className: 'w-[120px]',
      },
    },
    {
      id: 'actions',
      header: ({ column }) => (
        <DataTableColumnHeader column={column} title={t('Actions')} />
      ),
      cell: DataTableRowActions,
      enableColumnFilter: false,
      meta: {
        className: 'w-[60px]',
      },
    },
  ], [t])
}
