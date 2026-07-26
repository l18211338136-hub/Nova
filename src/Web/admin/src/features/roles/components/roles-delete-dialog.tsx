import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { ConfirmDialog } from '@/components/confirm-dialog'
import { type RoleDto as Role } from '@/api/model'
import { useDeleteRole } from '@/api/endpoints/roles'
import { useQueryClient } from '@tanstack/react-query'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  currentRow: Role
}

export function RolesDeleteDialog({ open, onOpenChange, currentRow }: Props) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  
  const deleteMutation = useDeleteRole({
    mutation: {
      onSuccess: () => {
        toast.success(t('Role deleted successfully'))
        queryClient.invalidateQueries({ queryKey: ['roles'] })
        onOpenChange(false)
      },
      onError: (error: any) => {
        toast.error(t('Failed to delete role'), {
          description: error?.response?.data?.title || error.message,
        })
      }
    }
  })

  return (
    <ConfirmDialog
      open={open}
      onOpenChange={onOpenChange}
      handleConfirm={() => {
        deleteMutation.mutate({ id: currentRow.id! })
      }}
      disabled={deleteMutation.isPending}
      title={
        <span className='text-destructive'>
          {t('Delete')} {currentRow.name}
        </span>
      }
      desc={t(
        'Are you sure you want to delete this role? This action cannot be undone.'
      )}
      confirmText={t('Delete')}
      destructive
    />
  )
}
