import { useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { type ColumnDef } from '@tanstack/react-table'
import { cn } from '@/lib/utils'
import { Badge } from '@/components/ui/badge'
import { Checkbox } from '@/components/ui/checkbox'
import { DataTableColumnHeader } from '@/components/data-table'
import { LongText } from '@/components/long-text'
import { type RoleDto as Role } from '@/api/model'
import { DataTableRowActions } from './data-table-row-actions'

export const useRolesColumns = () => {
  const { t } = useTranslation()
  
  return useMemo<ColumnDef<Role>[]>(() => [
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
      className: cn('inset-s-0 z-10 rounded-tl-[inherit] max-md:sticky w-[40px]'),
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
      <LongText className='max-w-36 ps-3'>{row.getValue('name')}</LongText>
    ),
    meta: {
      className: cn(
        'drop-shadow-[0_1px_2px_rgb(0_0_0_/_0.1)] dark:drop-shadow-[0_1px_2px_rgb(255_255_255_/_0.1)]',
        'inset-s-6 ps-0.5 max-md:sticky @4xl/content:table-cell @4xl/content:drop-shadow-none w-[180px]'
      ),
    },
    enableHiding: false,
  },
  {
    accessorKey: 'displayName',
    header: ({ column }) => (
      <DataTableColumnHeader column={column} title={t('Display Name')} />
    ),
    cell: ({ row }) => (
      <div className='w-fit ps-2 text-nowrap'>{row.getValue('displayName')}</div>
    ),
    meta: {
      className: 'w-[200px]',
    },
  },
  {
    accessorKey: 'isEnabled',
    header: ({ column }) => (
      <DataTableColumnHeader column={column} title={t('Status')} />
    ),
    cell: ({ row }) => {
      const isEnabled = row.getValue('isEnabled') as boolean;
      return (
        <div className='flex space-x-2'>
          <Badge variant='outline' className={cn('capitalize', isEnabled ? 'bg-green-100 text-green-700 dark:bg-green-900 dark:text-green-300' : 'bg-red-100 text-red-700 dark:bg-red-900 dark:text-red-300')}>
            {isEnabled ? t('Active') : t('Inactive')}
          </Badge>
        </div>
      )
    },
    enableHiding: false,
    meta: {
      filterType: 'boolean',
      className: 'w-[120px]',
    },
  },
  {
    accessorKey: 'createdAt',
    header: ({ column }) => (
      <DataTableColumnHeader column={column} title={t('Created At')} />
    ),
    cell: ({ row }) => {
      const date = row.getValue('createdAt') as Date;
      return <div>{new Date(date).toLocaleString()}</div>;
    },
    meta: { 
      filterType: 'date',
      className: 'w-[260px]',
    },
  },
  {
    id: 'actions',
    header: ({ column }) => (
      <DataTableColumnHeader column={column} title={t('Actions')} />
    ),
    cell: ({ row }) => <DataTableRowActions row={row} />,
    meta: {
      className: cn(
        'drop-shadow-[0_1px_2px_rgb(0_0_0_/_0.1)] dark:drop-shadow-[0_1px_2px_rgb(255_255_255_/_0.1)]',
        'inset-e-0 z-10 rounded-tr-[inherit] max-md:sticky @4xl/content:table-cell @4xl/content:drop-shadow-none w-[60px]'
      ),
    },
    enableSorting: false,
    enableHiding: false,
    enableColumnFilter: false,
  },
], [t])
}
