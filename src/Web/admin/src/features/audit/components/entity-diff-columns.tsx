import { useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { type ColumnDef } from '@tanstack/react-table'
import { Eye, User, Calendar } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { DataTableColumnHeader } from '@/components/data-table'
import type { EntityChangeLogDto } from '@/api/model'

interface UseEntityDiffColumnsProps {
  onViewDiff: (log: EntityChangeLogDto) => void
}

export const useEntityDiffColumns = ({ onViewDiff }: UseEntityDiffColumnsProps) => {
  const { t } = useTranslation()

  return useMemo<ColumnDef<EntityChangeLogDto>[]>(
    () => [
      {
        accessorKey: 'entityType',
        header: ({ column }) => (
          <DataTableColumnHeader column={column} title={t('实体类型')} />
        ),
        cell: ({ row }) => {
          const type = row.getValue('entityType') as string
          const labelMap: Record<string, string> = {
            User: t('用户 (User)'),
            Role: t('角色 (Role)'),
            Tenant: t('租户 (Tenant)'),
            Organization: t('组织 (Organization)'),
            Permission: t('权限 (Permission)'),
          }
          return (
            <Badge variant='outline' className='font-mono font-medium ps-1'>
              {labelMap[type] || type}
            </Badge>
          )
        },
        meta: {
          filterType: 'select',
          title: t('实体类型'),
          className: 'w-[170px]',
          selectOptions: [
            { value: 'User', label: t('用户 (User)') },
            { value: 'Role', label: t('角色 (Role)') },
            { value: 'Tenant', label: t('租户 (Tenant)') },
            { value: 'Organization', label: t('组织 (Organization)') },
            { value: 'Permission', label: t('权限 (Permission)') },
          ],
        },
      },
      {
        accessorKey: 'entityId',
        header: ({ column }) => (
          <DataTableColumnHeader column={column} title={t('显示名称 / 标识')} />
        ),
        cell: ({ row }) => (
          <span
            className='font-mono text-xs font-medium text-foreground max-w-[260px] truncate block ps-1'
            title={row.getValue('entityId')}
          >
            {row.getValue('entityId')}
          </span>
        ),
        meta: {
          filterType: 'text',
          title: t('显示名称 / 标识'),
          filterPlaceholder: t('实体标识'),
          className: 'w-[280px]',
        },
      },
      {
        accessorKey: 'changeType',
        header: ({ column }) => (
          <DataTableColumnHeader column={column} title={t('变更类型')} />
        ),
        cell: ({ row }) => {
          const changeType = row.getValue('changeType') as string
          const labelMap: Record<string, string> = {
            Added: t('新增 (Added)'),
            Modified: t('修改 (Modified)'),
            Deleted: t('删除 (Deleted)'),
          }
          return (
            <Badge
              className={
                changeType === 'Added'
                  ? 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border-emerald-500/20'
                  : changeType === 'Modified'
                  ? 'bg-blue-500/10 text-blue-600 dark:text-blue-400 border-blue-500/20'
                  : 'bg-rose-500/10 text-rose-600 dark:text-rose-400 border-rose-500/20'
              }
            >
              {labelMap[changeType] || changeType}
            </Badge>
          )
        },
        meta: {
          filterType: 'select',
          title: t('变更类型'),
          className: 'w-[150px]',
          selectOptions: [
            { value: 'Added', label: t('新增 (Added)') },
            { value: 'Modified', label: t('修改 (Modified)') },
            { value: 'Deleted', label: t('删除 (Deleted)') },
          ],
        },
      },
      {
        accessorKey: 'operatorName',
        header: ({ column }) => (
          <DataTableColumnHeader column={column} title={t('操作人')} />
        ),
        cell: ({ row }) => (
          <div className='flex items-center gap-1.5 font-mono text-xs text-muted-foreground ps-1'>
            <User className='h-3.5 w-3.5' />
            <span>{row.getValue('operatorName') || 'System'}</span>
          </div>
        ),
        meta: {
          filterType: 'text',
          title: t('操作人'),
          filterPlaceholder: t('操作人'),
          className: 'w-[140px]',
        },
      },
      {
        accessorKey: 'createdAt',
        header: ({ column }) => (
          <DataTableColumnHeader column={column} title={t('变更时间')} />
        ),
        cell: ({ row }) => {
          const date = row.getValue('createdAt') as string
          return (
            <div className='flex items-center gap-1.5 font-mono text-xs text-muted-foreground ps-1'>
              <Calendar className='h-3.5 w-3.5' />
              <span>{date ? new Date(date).toLocaleString() : '-'}</span>
            </div>
          )
        },
        meta: {
          filterType: 'date',
          title: t('变更时间'),
          className: 'w-[180px]',
        },
      },
      {
        id: 'actions',
        header: ({ column }) => (
          <DataTableColumnHeader column={column} title={t('操作')} />
        ),
        cell: ({ row }) => {
          const log = row.original
          return (
            <Button
              variant='outline'
              size='sm'
              className='h-8 gap-1.5 text-xs'
              onClick={() => onViewDiff(log)}
            >
              <Eye className='h-3.5 w-3.5 text-primary' />
              {t('Visual Diff 对照')}
            </Button>
          )
        },
        meta: {
          className: 'w-[140px]',
        },
      },
    ],
    [t, onViewDiff]
  )
}
