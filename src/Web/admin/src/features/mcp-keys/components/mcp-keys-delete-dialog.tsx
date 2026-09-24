'use client'

import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { AlertTriangle } from 'lucide-react'
import { useQueryClient } from '@tanstack/react-query'
import { useDeleteMcpKey } from '@/api/endpoints/mcp-keys'
import { toast } from 'sonner'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { ConfirmDialog } from '@/components/confirm-dialog'
import { type McpKeyDto as McpKey } from '@/api/model'

type McpKeysDeleteDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  currentRow: McpKey
}

export function McpKeysDeleteDialog({
  open,
  onOpenChange,
  currentRow,
}: McpKeysDeleteDialogProps) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const deleteMutation = useDeleteMcpKey()
  const [isLoading, setIsLoading] = useState(false)

  const handleDelete = () => {
    if (!currentRow?.id) return
    setIsLoading(true)
    deleteMutation.mutate(
      { id: currentRow.id },
      {
        onSuccess: () => {
          toast.success(t('MCP key deleted successfully'))
          onOpenChange(false)
          queryClient.invalidateQueries({ queryKey: ['mcp-keys'] })
        },
        onError: (err: { response?: { data?: { message?: string } } }) =>
          toast.error(err?.response?.data?.message || t('Failed to delete MCP key.')),
        onSettled: () => setIsLoading(false),
      }
    )
  }

  return (
    <ConfirmDialog
      open={open}
      onOpenChange={onOpenChange}
      handleConfirm={handleDelete}
      isLoading={isLoading}
      title={
        <span className='text-destructive'>
          <AlertTriangle
            className='me-1 inline-block stroke-destructive'
            size={18}
          />{' '}
          {t('Delete MCP Key')}
        </span>
      }
      desc={
        <div className='space-y-4'>
          <p>
            {t('Are you sure you want to delete')}{' '}
            <span className='font-bold'>{currentRow?.name}</span> ?
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
