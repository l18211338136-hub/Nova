'use client'

import { type Row } from '@tanstack/react-table'
import { DotsHorizontalIcon } from '@radix-ui/react-icons'
import { Eye, Pencil, Trash2 } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { usePermissions } from '@/hooks/use-permissions'
import { useMcpContext } from './mcp-provider'
import { type McpServerDto } from '../types'

type McpRowActionsProps<T extends { id?: string; name: string }> = {
  row: Row<T>
}

/**
 * MCP 服务行操作：详情（只读工具清单）/ 编辑 / 删除。
 *
 * 🔺 三个操作各自按权限条件渲染：
 * - 详情 → `Mcp.Tools.Read`
 * - 编辑 → `Mcp.Servers.Update`
 * - 删除 → `Mcp.Servers.Delete`
 * 三者都没有时返回 null，避免渲染一个空菜单。
 */
export function McpRowActions<T extends { id?: string; name: string }>({
  row,
}: McpRowActionsProps<T>) {
  const { t } = useTranslation()
  const { setOpen, setCurrentRow } = useMcpContext()
  const { hasPermission } = usePermissions()

  const canRead = hasPermission('Mcp.Tools.Read')
  const canUpdate = hasPermission('Mcp.Servers.Update')
  const canDelete = hasPermission('Mcp.Servers.Delete')

  if (!canRead && !canUpdate && !canDelete) return null

  const selectRow = () =>
    setCurrentRow(row.original as unknown as McpServerDto)

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
      <DropdownMenuContent align='end' className='w-44'>
        {canRead && (
          <DropdownMenuItem
            onClick={() => {
              selectRow()
              setOpen('detail-server')
            }}
          >
            <Eye className='mr-2 h-4 w-4' />
            {t('Details')}
          </DropdownMenuItem>
        )}
        {canUpdate && (
          <DropdownMenuItem
            onClick={() => {
              selectRow()
              setOpen('edit-server')
            }}
          >
            <Pencil className='mr-2 h-4 w-4' />
            {t('Edit')}
          </DropdownMenuItem>
        )}
        {canDelete && (
          <>
            {(canRead || canUpdate) && <DropdownMenuSeparator />}
            <DropdownMenuItem
              onClick={() => {
                selectRow()
                setOpen('delete-server')
              }}
              className='text-red-500 focus:text-red-500'
            >
              <Trash2 className='mr-2 h-4 w-4' />
              {t('Delete')}
            </DropdownMenuItem>
          </>
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
