import { useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { type ColumnDef } from '@tanstack/react-table'
import { cn } from '@/lib/utils'
import { Badge } from '@/components/ui/badge'
import { DataTableColumnHeader } from '@/components/data-table'
import { LongText } from '@/components/long-text'
import { type AuthAuditLogDto as AuthAuditLog } from '@/api/model'

export const useAuditColumns = () => {
  const { t } = useTranslation()

  return useMemo<ColumnDef<AuthAuditLog>[]>(
    () => [
      {
        accessorKey: 'eventType',
        header: ({ column }) => (
          <DataTableColumnHeader column={column} title={t('Event')} />
        ),
        cell: ({ row }) => {
          const evt = (row.getValue('eventType') as string) ?? ''
          return (
            <Badge variant='secondary' className='font-medium'>
              {t(evt)}
            </Badge>
          )
        },
        meta: {
          filterType: 'select',
          className: 'w-[180px]',
          selectOptions: [
            { value: 'LoginSuccess', label: t('LoginSuccess') },
            { value: 'LoginFailed', label: t('LoginFailed') },
            { value: 'TokenRefreshed', label: t('TokenRefreshed') },
            { value: 'PasswordChanged', label: t('PasswordChanged') },
            { value: 'Logout', label: t('Logout') },
          ],
        },
      },
      {
        accessorKey: 'account',
        header: ({ column }) => (
          <DataTableColumnHeader column={column} title={t('Account')} />
        ),
        cell: ({ row }) => (
          <div className='ps-2 text-nowrap'>
            {row.getValue('account') || '-'}
          </div>
        ),
        meta: {
          filterType: 'text',
          className: 'w-[200px]',
          filterPlaceholder: t('Account'),
        },
      },
      {
        accessorKey: 'success',
        header: ({ column }) => (
          <DataTableColumnHeader column={column} title={t('Result')} />
        ),
        cell: ({ row }) => {
          const success = row.getValue('success') as boolean
          return (
            <Badge
              variant='outline'
              className={cn(
                'capitalize',
                success
                  ? 'bg-green-100 text-green-700 dark:bg-green-900 dark:text-green-300'
                  : 'bg-red-100 text-red-700 dark:bg-red-900 dark:text-red-300'
              )}
            >
              {success ? t('Success') : t('Failed')}
            </Badge>
          )
        },
        enableSorting: true,
        meta: {
          filterType: 'boolean',
          className: 'w-[110px]',
          booleanOptions: {
            trueLabel: t('Success'),
            falseLabel: t('Failed'),
          },
        },
      },
      {
        accessorKey: 'reason',
        header: ({ column }) => (
          <DataTableColumnHeader column={column} title={t('Reason')} />
        ),
        cell: ({ row }) => (
          <LongText className='max-w-60 ps-2'>
            {row.getValue('reason') || '-'}
          </LongText>
        ),
        enableSorting: false,
        enableColumnFilter: false,
        meta: {
          className: 'w-[240px]',
        },
      },
      {
        accessorKey: 'ipAddress',
        header: ({ column }) => (
          <DataTableColumnHeader column={column} title={t('IP Address')} />
        ),
        cell: ({ row }) => (
          <div className='w-fit ps-2 text-nowrap'>
            {row.getValue('ipAddress') || '-'}
          </div>
        ),
        meta: {
          filterType: 'text',
          className: 'w-[160px]',
          filterPlaceholder: t('IP Address'),
        },
      },
      {
        accessorKey: 'occurredOn',
        header: ({ column }) => (
          <DataTableColumnHeader column={column} title={t('Time')} />
        ),
        cell: ({ row }) => {
          const date = row.getValue('occurredOn') as string
          return (
            <div className='ps-2 text-nowrap'>
              {date ? new Date(date).toLocaleString() : '-'}
            </div>
          )
        },
        meta: {
          filterType: 'date',
          className: 'w-[220px]',
        },
      },
    ],
    [t]
  )
}
