'use client'

import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { type Table } from '@tanstack/react-table'
import { AlertTriangle } from 'lucide-react'
import { toast } from 'sonner'
import { useQueryClient } from '@tanstack/react-query'
import { useDeleteUser, getUsersQueryKey } from '@/api/endpoints/users'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { ConfirmDialog } from '@/components/confirm-dialog'

type UserMultiDeleteDialogProps<TData> = {
  open: boolean
  onOpenChange: (open: boolean) => void
  table: Table<TData>
}

const CONFIRM_WORD = 'DELETE'

export function UsersMultiDeleteDialog<TData>({
  open,
  onOpenChange,
  table,
}: UserMultiDeleteDialogProps<TData>) {
  const { t } = useTranslation()
  const [value, setValue] = useState('')
  const queryClient = useQueryClient()
  const deleteMutation = useDeleteUser()

  const selectedRows = table.getFilteredSelectedRowModel().rows

  const handleDelete = () => {
    if (value.trim() !== CONFIRM_WORD) {
      toast.error(t('Please type "{{word}}" to confirm.', { word: CONFIRM_WORD }))
      return
    }

    onOpenChange(false)

    const deletePromises = selectedRows.map((row) =>
      deleteMutation.mutateAsync({ id: (row.original as any).id! })
    )

    toast.promise(Promise.all(deletePromises), {
      loading: t('Deleting users...'),
      success: () => {
        setValue('')
        table.resetRowSelection()
        queryClient.invalidateQueries({ queryKey: ['users'] })
        return t('Deleted {{count}} {{item}}', { count: selectedRows.length, item: selectedRows.length > 1 ? t('users') : t('user') })
      },
      error: t('Error deleting users'),
    })
  }

  return (
    <ConfirmDialog
      open={open}
      onOpenChange={onOpenChange}
      form='users-multi-delete-form'
      disabled={value.trim() !== CONFIRM_WORD}
      title={
        <span className='text-destructive'>
          <AlertTriangle
            className='me-1 inline-block stroke-destructive'
            size={18}
          />{' '}
          {t('Delete {{count}} {{item}}', { count: selectedRows.length, item: selectedRows.length > 1 ? t('users') : t('user') })}
        </span>
      }
      desc={
        <form
          id='users-multi-delete-form'
          onSubmit={(e) => {
            e.preventDefault()
            handleDelete()
          }}
          className='space-y-4'
        >
          <p className='mb-2'>
            {t('Are you sure you want to delete the selected users?')} <br />
            {t('This action cannot be undone.')}
          </p>

          <Label className='my-4 flex flex-col items-start gap-1.5'>
            <span className=''>{t('Confirm by typing "{{word}}":', { word: CONFIRM_WORD })}</span>
            <Input
              value={value}
              onChange={(e) => setValue(e.target.value)}
              placeholder={t('Type "{{word}}" to confirm.', { word: CONFIRM_WORD })}
              autoFocus
            />
          </Label>

          <Alert variant='destructive'>
            <AlertTitle>{t('Warning!')}</AlertTitle>
            <AlertDescription>
              {t('Please be careful, this operation can not be rolled back.')}
            </AlertDescription>
          </Alert>
        </form>
      }
      confirmText={t('Delete')}
      destructive
    />
  )
}
