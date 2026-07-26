import { Row } from '@tanstack/react-table'
import { Edit, Trash } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { TenantDto as Tenant } from '@/api/model'
import { useTenantsContext } from './tenants-provider'
import { useTranslation } from 'react-i18next'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { DotsHorizontalIcon } from '@radix-ui/react-icons'

interface DataTableRowActionsProps {
  row: Row<Tenant>
}

export function DataTableRowActions({ row }: DataTableRowActionsProps) {
  const { setOpen, setCurrentRow } = useTenantsContext()
  const { t } = useTranslation()

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          variant='ghost'
          className='flex h-8 w-8 p-0 data-[state=open]:bg-muted'
        >
          <DotsHorizontalIcon className='h-4 w-4' />
          <span className='sr-only'>{t('Open menu')}</span>
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align='end' className='w-[160px]'>
        <DropdownMenuItem
          onClick={() => {
            setCurrentRow(row.original)
            setOpen('update')
          }}
        >
          <Edit className='mr-2 h-4 w-4 text-blue-500' />
          {t('Edit')}
        </DropdownMenuItem>
        <DropdownMenuItem
          onClick={() => {
            setCurrentRow(row.original)
            setOpen('delete')
          }}
        >
          <Trash className='mr-2 h-4 w-4 text-red-500' />
          {t('Delete')}
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
