import { ColumnDef } from '@tanstack/react-table'
import { Checkbox } from '@/components/ui/checkbox'
import { TenantDto as Tenant } from '@/api/model'
import { DataTableColumnHeader } from '@/components/data-table'
import { DataTableRowActions } from './data-table-row-actions'
import { useTranslation } from 'react-i18next'
import { format } from 'date-fns'

export function useTenantsColumns(): ColumnDef<Tenant>[] {
  const { t } = useTranslation()

  return [
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
          className='translate-y-[2px]'
        />
      ),
      cell: ({ row }) => (
        <Checkbox
          checked={row.getIsSelected()}
          onCheckedChange={(value) => row.toggleSelected(!!value)}
          aria-label='Select row'
          className='translate-y-[2px]'
        />
      ),
      enableSorting: false,
      enableHiding: false,
      meta: {
        className: 'w-[40px]',
      },
    },
    {
      accessorKey: 'id',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('Tenant ID')} />,
      cell: ({ row }) => <div className='font-mono'>{row.getValue('id')}</div>,
      meta: {
        className: 'w-[150px]',
      },
    },
    {
      accessorKey: 'name',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('Name')} />,
      cell: ({ row }) => <div>{row.getValue('name')}</div>,
    },
    {
      accessorKey: 'identifier',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('Identifier')} />,
      cell: ({ row }) => <div>{row.getValue('identifier')}</div>,
    },
    {
      accessorKey: 'adminEmail',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('Admin Email')} />,
      cell: ({ row }) => <div>{row.getValue('adminEmail')}</div>,
    },
    {
      accessorKey: 'isActive',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('Status')} />,
      cell: ({ row }) => {
        const isActive = row.getValue('isActive')
        return (
          <div className='flex items-center'>
            <span
              className={`inline-block h-2 w-2 rounded-full mr-2 ${isActive ? 'bg-green-500' : 'bg-red-500'
                }`}
            ></span>
            {isActive ? t('Active') : t('Inactive')}
          </div>
        )
      },
      meta: {
        filterType: 'boolean',
      },
    },
    {
      accessorKey: 'validUpto',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('Valid Upto')} />,
      cell: ({ row }) => {
        const dateStr = row.getValue('validUpto') as string
        if (!dateStr) return '-'
        try {
          return format(new Date(dateStr), 'yyyy-MM-dd')
        } catch {
          return dateStr
        }
      },
      meta: {
        filterType: 'date',
        className: 'w-[260px]',
      },
    },
    {
      id: 'actions',
      header: () => <DataTableColumnHeader column={{ getCanSort: () => false, getCanFilter: () => false } as any} title={t('Actions')} />,
      cell: ({ row }) => <DataTableRowActions row={row} />,
      enableSorting: false,
      enableHiding: false,
      meta: {
        className: 'w-[60px]',
      },
    },
  ]
}
