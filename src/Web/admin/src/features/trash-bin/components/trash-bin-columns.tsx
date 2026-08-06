import { useMemo } from 'react'
import { type ColumnDef } from '@tanstack/react-table'
import { RotateCcw, Trash2, User } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { DataTableColumnHeader } from '@/components/data-table'
import type { TrashBinItemDto } from '@/api/model'

interface ColumnActionsProps {
  onRestore: (item: TrashBinItemDto) => void
  onHardDelete: (item: TrashBinItemDto) => void
}

export function useTrashBinColumns({
  onRestore,
  onHardDelete,
}: ColumnActionsProps) {
  const { t } = useTranslation()

  return useMemo<ColumnDef<TrashBinItemDto>[]>(
    () => [
      {
        accessorKey: 'entityType',
        header: ({ column }) => (
          <DataTableColumnHeader column={column} title={t('Entity Type')} />
        ),
        cell: ({ row }) => {
          const type = row.original.entityType || 'Unknown'
          switch (type.toLowerCase()) {
            case 'user':
              return (
                <Badge className='bg-blue-500/10 text-blue-500 hover:bg-blue-500/20 border-blue-500/20'>
                  {t('User (User)')}
                </Badge>
              )
            case 'role':
              return (
                <Badge className='bg-purple-500/10 text-purple-500 hover:bg-purple-500/20 border-purple-500/20'>
                  {t('Role (Role)')}
                </Badge>
              )
            case 'menu':
              return (
                <Badge className='bg-emerald-500/10 text-emerald-500 hover:bg-emerald-500/20 border-emerald-500/20'>
                  {t('Menu (Menu)')}
                </Badge>
              )
            default:
              return <Badge variant='outline'>{type}</Badge>
          }
        },
        meta: {
          filterType: 'select',
          title: t('Entity Type'),
          className: 'w-[160px]',
          selectOptions: [
            { label: t('User (User)'), value: 'User' },
            { label: t('Role (Role)'), value: 'Role' },
            { label: t('Menu (Menu)'), value: 'Menu' },
          ],
        },
      },
      {
        accessorKey: 'displayName',
        header: ({ column }) => (
          <DataTableColumnHeader column={column} title={t('Display Name / Identifier')} />
        ),
        cell: ({ row }) => (
          <span className='font-medium text-foreground'>
            {row.original.displayName || row.original.id}
          </span>
        ),
        meta: {
          title: t('Display Name / Identifier'),
          className: 'w-[240px]',
          filterType: 'text',
        },
      },
      {
        accessorKey: 'deletedBy',
        header: ({ column }) => (
          <DataTableColumnHeader column={column} title={t('Deleted By')} />
        ),
        cell: ({ row }) => {
          const operator = row.original.deletedByUserName || row.original.deletedBy || 'System'
          return (
            <div className='flex items-center gap-1.5 text-sm text-foreground font-mono'>
              <User className='h-3.5 w-3.5 text-muted-foreground/70' />
              <span className='truncate max-w-[140px]' title={operator}>
                {operator}
              </span>
            </div>
          )
        },
        meta: {
          title: t('Deleted By'),
          className: 'w-[160px]',
          filterType: 'text',
        },
      },
      {
        accessorKey: 'deletedAt',
        header: ({ column }) => (
          <DataTableColumnHeader column={column} title={t('Deleted Time')} />
        ),
        cell: ({ row }) => {
          const dateStr = row.original.deletedAt
          if (!dateStr) return <span className='text-muted-foreground text-xs'>-</span>
          return (
            <span className='text-muted-foreground text-sm font-mono'>
              {new Date(dateStr).toLocaleString()}
            </span>
          )
        },
        meta: {
          title: t('Deleted Time'),
          className: 'w-[200px]',
          filterType: 'date',
        },
      },
      {
        id: 'actions',
        header: ({ column }) => (
          <DataTableColumnHeader column={column} title={t('Actions')} className='justify-end' />
        ),
        cell: ({ row }) => {
          const item = row.original
          return (
            <div className='flex items-center justify-end gap-2'>
              <Button
                size='sm'
                variant='outline'
                className='h-8 text-emerald-600 hover:text-emerald-700 hover:bg-emerald-50 dark:hover:bg-emerald-950/30'
                onClick={() => onRestore(item)}
              >
                <RotateCcw className='mr-1 h-3.5 w-3.5' />
                {t('Restore')}
              </Button>
              <Button
                size='sm'
                variant='outline'
                className='h-8 text-destructive hover:text-destructive hover:bg-destructive/10'
                onClick={() => onHardDelete(item)}
              >
                <Trash2 className='mr-1 h-3.5 w-3.5' />
                {t('Hard Delete')}
              </Button>
            </div>
          )
        },
        enableColumnFilter: false,
        meta: {
          title: t('Actions'),
          className: 'w-[180px]',
        },
      },
    ],
    [onHardDelete, onRestore, t]
  )
}
