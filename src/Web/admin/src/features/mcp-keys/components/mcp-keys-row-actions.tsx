import { DotsHorizontalIcon } from '@radix-ui/react-icons'
import { useTranslation } from 'react-i18next'
import { type Row } from '@tanstack/react-table'
import { Pencil, Trash2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { type McpKeyDto as McpKey } from '@/api/model'
import { useMcpKeysContext } from './mcp-keys-provider'
import { usePermissions } from '@/hooks/use-permissions'

type McpKeysRowActionsProps = {
  row: Row<McpKey>
}

export function DataTableRowActions({ row }: McpKeysRowActionsProps) {
  const { t } = useTranslation()
  const { setOpen, setCurrentRow } = useMcpKeysContext()
  const { hasPermission } = usePermissions()

  const canUpdate = hasPermission('Mcp.Keys.Update')
  const canDelete = hasPermission('Mcp.Keys.Delete')

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
          <DotsHorizontalIcon className='h-4 w-4' />
          <span className='sr-only'>{t('Open menu')}</span>
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align='end' className='w-36'>
        {canUpdate && (
          <DropdownMenuItem
            onClick={() => {
              setCurrentRow(row.original)
              setOpen('edit')
            }}
          >
            <Pencil className='mr-2 h-4 w-4 text-primary' />
            {t('Edit')}
          </DropdownMenuItem>
        )}
        {canDelete && (
          <DropdownMenuItem
            className='text-red-500 focus:text-red-500'
            onClick={() => {
              setCurrentRow(row.original)
              setOpen('delete')
            }}
          >
            <Trash2 className='mr-2 h-4 w-4' />
            {t('Delete')}
          </DropdownMenuItem>
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
