import { useMemo } from 'react'
import { type ColumnDef } from '@tanstack/react-table'
import { Checkbox } from '@/components/ui/checkbox'
import { Badge } from '@/components/ui/badge'
import { MenuDto } from './menus-provider'
import { ChevronRight, ChevronDown, Plus, Edit, Trash } from 'lucide-react'
import * as Icons from 'lucide-react'
import { Button } from '@/components/ui/button'
import { useMenusContext } from './menus-provider'
import { useTranslation } from 'react-i18next'
import { usePermissions } from '@/hooks/use-permissions'

import { cn } from '@/lib/utils'
import { DataTableColumnHeader } from '@/components/data-table'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { MoreHorizontal } from 'lucide-react'

const DataTableRowActions = ({ row }: { row: any }) => {
  const { setOpen, setCurrentRow } = useMenusContext()
  const { hasPermission } = usePermissions()
  const { t } = useTranslation()

  const canUpdate = hasPermission('Identity.Menus.Update')
  const canDelete = hasPermission('Identity.Menus.Delete')

  if (!canUpdate && !canDelete) {
    return null
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" className="h-8 w-8 p-0">
          <span className="sr-only">Open menu</span>
          <MoreHorizontal className="h-4 w-4" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-[160px]">
        <DropdownMenuLabel>{t('Actions')}</DropdownMenuLabel>
        <DropdownMenuItem onClick={() => { setCurrentRow(row.original); setOpen('create-sub') }}>
          <Plus className="mr-2 h-4 w-4" />
          {t('Add Child Menu')}
        </DropdownMenuItem>
        {canUpdate && (
          <DropdownMenuItem onClick={() => { setCurrentRow(row.original); setOpen('edit') }}>
            <Edit className="mr-2 h-4 w-4" />
            {t('Edit')}
          </DropdownMenuItem>
        )}
        {canUpdate && canDelete && <DropdownMenuSeparator />}
        {canDelete && (
          <DropdownMenuItem onClick={() => { setCurrentRow(row.original); setOpen('delete') }} className="text-red-600">
            <Trash className="mr-2 h-4 w-4" />
            {t('Delete')}
          </DropdownMenuItem>
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}

export const useMenusColumns = () => {
  const { t } = useTranslation()

  return useMemo<ColumnDef<MenuDto>[]>(() => [
  {
    id: 'select',
    header: ({ table }) => (
      <Checkbox
        checked={table.getIsAllPageRowsSelected() || (table.getIsSomePageRowsSelected() && 'indeterminate')}
        onCheckedChange={(value) => table.toggleAllPageRowsSelected(!!value)}
        aria-label="Select all"
        className="translate-y-[2px]"
      />
    ),
    meta: {
      className: cn('inset-s-0 z-10 rounded-tl-[inherit] max-md:sticky w-[40px]'),
    },
    cell: ({ row }) => (
      <div className="flex items-center">
        <Checkbox
          checked={row.getIsSelected()}
          onCheckedChange={(value) => row.toggleSelected(!!value)}
          aria-label="Select row"
          className="translate-y-[2px] mr-2"
        />
      </div>
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
    cell: ({ row }) => {
      const iconName = row.original.icon as keyof typeof Icons
      const IconComponent = iconName ? Icons[iconName] as React.ElementType : null
      
      return (
        <div
          className="flex items-center gap-2"
          style={{ paddingLeft: `${row.depth * 2}rem` }}
        >
          {row.getCanExpand() ? (
            <button
              className="cursor-pointer"
              onClick={row.getToggleExpandedHandler()}
            >
              {row.getIsExpanded() ? (
                <ChevronDown className="h-4 w-4" />
              ) : (
                <ChevronRight className="h-4 w-4" />
              )}
            </button>
          ) : (
            <span className="w-4" />
          )}
          {IconComponent && <IconComponent className="h-4 w-4 text-muted-foreground" />}
          <span className="font-medium">{row.getValue('name')}</span>
        </div>
      )
    },
    meta: {
      className: cn(
        'drop-shadow-[0_1px_2px_rgb(0_0_0_/_0.1)] dark:drop-shadow-[0_1px_2px_rgb(255_255_255_/_0.1)]',
        'inset-s-6 ps-0.5 max-md:sticky @4xl/content:table-cell @4xl/content:drop-shadow-none w-[200px]'
      ),
    },
    enableHiding: false,
  },
  {
    accessorKey: 'path',
    header: ({ column }) => (
      <DataTableColumnHeader column={column} title={t('Path')} />
    ),
    meta: { className: 'w-[150px]' },
  },
  {
    accessorKey: 'component',
    header: ({ column }) => (
      <DataTableColumnHeader column={column} title={t('Component')} />
    ),
    meta: { className: 'w-[200px]' },
  },
  {
    accessorKey: 'sort',
    header: ({ column }) => (
      <DataTableColumnHeader column={column} title={t('Sort')} />
    ),
    meta: { className: 'w-[100px]' },
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
    id: 'actions',
    header: ({ column }) => (
      <DataTableColumnHeader column={column} title={t('Actions')} />
    ),
    cell: ({ row }) => <DataTableRowActions row={row} />,
    enableColumnFilter: false,
    meta: {
      className: 'w-[60px]',
    },
  },
  ], [t])
}
