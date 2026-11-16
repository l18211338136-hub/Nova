'use client'

import { useTranslation } from 'react-i18next'
import { AlertTriangle } from 'lucide-react'
import { useQueryClient } from '@tanstack/react-query'
import { useDeleteMcpServer, useDeleteMcpTool } from '@/api/endpoints/mcp'
import { toast } from 'sonner'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { ConfirmDialog } from '@/components/confirm-dialog'
import { useMcpContext } from './mcp-provider'

type Kind = 'server' | 'tool'

type McpDeleteDialogProps = {
  kind: Kind
}

export function McpDeleteDialog({ kind }: McpDeleteDialogProps) {
  const { open, setOpen, currentRow, setCurrentRow } = useMcpContext()
  const { t } = useTranslation()
  const queryClient = useQueryClient()

  // Both hooks are called unconditionally (rules of hooks); we pick the right one.
  const deleteServer = useDeleteMcpServer()
  const deleteTool = useDeleteMcpTool()
  const mutation = kind === 'server' ? deleteServer : deleteTool

  const openKey = kind === 'server' ? 'delete-server' : 'delete-tool'
  const isOpen = open === openKey
  const name = currentRow?.name ?? ''

  const handleDelete = () => {
    if (!currentRow) return

    mutation.mutate(
      { id: currentRow.id! },
      {
        onSuccess: () => {
          toast.success(
            kind === 'server'
              ? t('MCP server deleted successfully')
              : t('MCP tool deleted successfully')
          )
          setOpen(null)
          setCurrentRow(null)
          queryClient.invalidateQueries({ queryKey: ['mcp-servers'] })
          queryClient.invalidateQueries({ queryKey: ['mcp-tools'] })
        },
        onError: (error: any) =>
          toast.error(error?.response?.data?.message || t('Delete failed.')),
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
      isLoading={mutation.isPending}
      title={
        <span className='text-destructive'>
          <AlertTriangle
            className='me-1 inline-block stroke-destructive'
            size={18}
          />{' '}
          {kind === 'server' ? t('Delete MCP Server') : t('Delete MCP Tool')}
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
