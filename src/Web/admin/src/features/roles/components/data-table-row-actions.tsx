import { useTranslation } from 'react-i18next'
import { Row } from '@tanstack/react-table'
import { Edit, ShieldAlert, Trash2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuShortcut,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { type RoleDto as Role } from '@/api/model'
import { useRolesContext } from './roles-provider'
import { usePermissions } from '@/hooks/use-permissions'

interface DataTableRowActionsProps {
  row: Row<Role>
}

export function DataTableRowActions({ row }: DataTableRowActionsProps) {
  const { setOpen, setCurrentRow } = useRolesContext()
  const { t } = useTranslation()
  const { hasPermission } = usePermissions()

  const canUpdate = hasPermission('Identity.Roles.Update')
  const canDelete = hasPermission('Identity.Roles.Delete')

  if (!canUpdate && !canDelete) {
    return null
  }

  return (
    <DropdownMenu modal={false}>
      <DropdownMenuTrigger asChild>
        <Button
          variant='ghost'
          className='flex h-8 w-8 p-0 data-[state=open]:bg-muted'
        >
          <span className='sr-only'>{t('Open menu')}</span>
          <Edit className='h-4 w-4' />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align='end' className='w-[160px]'>
        {canUpdate && (
          <DropdownMenuItem
            onClick={() => {
              setCurrentRow(row.original)
              setOpen('edit')
            }}
          >
            <Edit className='mr-2 h-4 w-4 text-primary' />
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
            <ShieldAlert className='mr-2 h-4 w-4 text-emerald-500' />
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
          >
            <Trash2 className='mr-2 h-4 w-4 text-red-500' />
            <span className='text-red-500'>{t('Delete')}</span>
            <DropdownMenuShortcut>⌘⌫</DropdownMenuShortcut>
          </DropdownMenuItem>
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
