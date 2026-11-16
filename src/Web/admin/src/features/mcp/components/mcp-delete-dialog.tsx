'use client'

import { useTranslation } from 'react-i18next'
import { AlertTriangle } from 'lucide-react'
import { useQueryClient } from '@tanstack/react-query'
import { useDeleteMcpServer } from '@/api/endpoints/mcp'
import { toast } from 'sonner'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { ConfirmDialog } from '@/components/confirm-dialog'
import { useMcpContext } from './mcp-provider'

/**
 * 删除 MCP 服务。
 *
 * 「MCP 工具」tab 下线后不再提供删除单个工具的入口，因此这里只处理服务维度；
 * 后端 `DELETE /api/mcp/tools/{id}` 端点保留不动（二期如需再接入）。
 */
export function McpDeleteDialog() {
  const { open, setOpen, currentRow, setCurrentRow } = useMcpContext()
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const deleteServer = useDeleteMcpServer()

  const isOpen = open === 'delete-server'
  const name = currentRow?.name ?? ''

  const handleDelete = () => {
    if (!currentRow?.id) return

    deleteServer.mutate(
      { id: currentRow.id },
      {
        onSuccess: () => {
          toast.success(t('MCP server deleted successfully'))
          setOpen(null)
          setCurrentRow(null)
          queryClient.invalidateQueries({ queryKey: ['mcp-servers'] })
          queryClient.invalidateQueries({ queryKey: ['mcp-tools'] })
        },
        onError: (error: unknown) =>
          toast.error(
            (error as { response?: { data?: { message?: string } } })?.response
              ?.data?.message || t('Delete failed.')
          ),
      }
    )
  }

  return (
    <ConfirmDialog
      open={isOpen}
      onOpenChange={(o) => {
        if (!o) setOpen(null)
      }}
      handleConfirm={handleDelete}
      isLoading={deleteServer.isPending}
      title={
        <span className='text-destructive'>
          <AlertTriangle
            className='me-1 inline-block stroke-destructive'
            size={18}
          />{' '}
          {t('Delete MCP Server')}
        </span>
      }
      desc={
        <div className='space-y-4'>
          <p>
            {t('Are you sure you want to delete')}{' '}
            <span className='font-bold'>{name}</span>?
            <br />
            {t('This action cannot be undone.')}
          </p>

          <Alert variant='destructive'>
            <AlertTitle>{t('Warning!')}</AlertTitle>
            <AlertDescription>
              {t('Please be careful, this operation can not be rolled back.')}
            </AlertDescription>
          </Alert>
        </div>
      }
      confirmText={t('Delete')}
      destructive
    />
  )
}
