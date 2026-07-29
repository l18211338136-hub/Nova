import { DotsHorizontalIcon } from '@radix-ui/react-icons'
import { useTranslation } from 'react-i18next'
import { type Row } from '@tanstack/react-table'
import { ShieldAlert, Trash2, UserPen } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuShortcut,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { type UserDto as User } from '@/api/model'
import { useUsersContext } from './users-provider'
import { usePermissions } from '@/hooks/use-permissions'

type DataTableRowActionsProps = {
  row: Row<User>
}

export function DataTableRowActions({ row }: DataTableRowActionsProps) {
  const { t } = useTranslation()
  const { setOpen, setCurrentRow } = useUsersContext()
  const { hasPermission } = usePermissions()

  const canUpdate = hasPermission('Identity.Users.Update')
  const canDelete = hasPermission('Identity.Users.Delete')

  if (!canUpdate && !canDelete) {
    return null
  }

  return (
    <>
      <DropdownMenu modal={false}>
        <DropdownMenuTrigger asChild>
          <Button
            variant='ghost'
            className='flex h-8 w-8 p-0 data-[state=open]:bg-muted'
          >
            <DotsHorizontalIcon className='h-4 w-4' />
            <span className='sr-only'>{t('Open menu')}</span>
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align='end' className='w-40'>
          {canUpdate && (
            <DropdownMenuItem
              onClick={() => {
                setCurrentRow(row.original)
                setOpen('edit')
              }}
            >
              <UserPen className="mr-2 h-4 w-4 text-primary" />
              {t('Edit')}
            </DropdownMenuItem>
          )}
          {canUpdate && (
            <DropdownMenuItem
              onClick={() => {
                setCurrentRow(row.original)
                setOpen('permissions')
              }}
            >
              <ShieldAlert className="mr-2 h-4 w-4 text-emerald-500" />
              <span className="text-emerald-500">{t('权限分配')}</span>
            </DropdownMenuItem>
          )}
          {canUpdate && canDelete && <DropdownMenuSeparator />}
          {canDelete && (
            <DropdownMenuItem
              onClick={() => {
                setCurrentRow(row.original)
                setOpen('delete')
              }}
              className='text-red-500 focus:text-red-500'
            >
              <Trash2 className="mr-2 h-4 w-4" />
              {t('Delete')}
              <DropdownMenuShortcut>⌘⌫</DropdownMenuShortcut>
            </DropdownMenuItem>
          )}
        </DropdownMenuContent>
      </DropdownMenu>
    </>
  )
}
