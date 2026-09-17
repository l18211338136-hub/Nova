'use client'

import { type Row } from '@tanstack/react-table'
import { DotsHorizontalIcon } from '@radix-ui/react-icons'
import { Trash2 } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { usePermissions } from '@/hooks/use-permissions'
import { useMcpContext } from './mcp-provider'
import { type McpServerDto, type McpToolDto } from '../types'

type Kind = 'server' | 'tool'

type McpRowActionsProps<T extends { id?: string; name: string }> = {
  row: Row<T>
  kind: Kind
}

export function McpRowActions<T extends { id?: string; name: string }>({
  row,
  kind,
}: McpRowActionsProps<T>) {
  const { t } = useTranslation()
  const { setOpen, setCurrentRow } = useMcpContext()
  const { hasPermission } = usePermissions()

  const canDelete = hasPermission(
    kind === 'server' ? 'Mcp.Servers.Delete' : 'Mcp.Tools.Delete'
  )

  if (!canDelete) return null

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
      <DropdownMenuContent align='end' className='w-40'>
        <DropdownMenuItem
          onClick={() => {
            setCurrentRow(row.original as McpServerDto | McpToolDto)
            setOpen(kind === 'server' ? 'delete-server' : 'delete-tool')
          }}
          className='text-red-500 focus:text-red-500'
        >
          <Trash2 className='mr-2 h-4 w-4' />
          {t('Delete')}
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
