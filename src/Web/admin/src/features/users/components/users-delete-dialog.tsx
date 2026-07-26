'use client'

import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { AlertTriangle } from 'lucide-react'
import { useQueryClient } from '@tanstack/react-query'
import { useDeleteUser, getUsersQueryKey } from '@/api/endpoints/users'
import { toast } from 'sonner'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { ConfirmDialog } from '@/components/confirm-dialog'
import { type UserDto as User } from '@/api/model'

type UserDeleteDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  currentRow: User
}

export function UsersDeleteDialog({
  open,
  onOpenChange,
  currentRow,
}: UserDeleteDialogProps) {
  const { t } = useTranslation()
  const [value, setValue] = useState('')
  const queryClient = useQueryClient()
  const deleteMutation = useDeleteUser()

  const handleDelete = () => {
    if (value.trim() !== currentRow.userName) return

    deleteMutation.mutate(
      { id: currentRow.id! },
      {
        onSuccess: () => {
          toast.success(t('User deleted successfully'))
          onOpenChange(false)
          queryClient.invalidateQueries({ queryKey: ['users'] })
        },
      }
    )
  }

  return (
    <ConfirmDialog
      open={open}
      onOpenChange={onOpenChange}
      form='users-delete-form'
      disabled={value.trim() !== currentRow.userName}
      title={
        <span className='text-destructive'>
          <AlertTriangle
            className='me-1 inline-block stroke-destructive'
            size={18}
          />{' '}
          {t('Delete User')}
        </span>
      }
      desc={
        <form
          id='users-delete-form'
          onSubmit={(e) => {
            e.preventDefault()
            handleDelete()
          }}
          className='space-y-4'
        >
          <p className='mb-2'>
            {t('Are you sure you want to delete')} <span className='font-bold'>{currentRow.userName}</span> ?
            <br />
            {t('This action will permanently remove the user with the role of')}{' '}
            <span className='font-bold'>
              User
            </span>{' '}
            {t('from the system. This cannot be undone.')}
          </p>

          <Label className='my-2'>
            {t('Username')}:
            <Input
              value={value}
              onChange={(e) => setValue(e.target.value)}
              placeholder={t('Enter username to confirm deletion.')}
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
