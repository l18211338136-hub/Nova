import { useTranslation } from 'react-i18next'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { TenantDto as Tenant } from '@/api/model'
import { useDeleteTenant } from '@/api/endpoints/tenants'
import { useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { useState } from 'react'

interface TenantsDeleteDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  currentRow?: Tenant | null
}

export function TenantsDeleteDialog({ open, onOpenChange, currentRow }: TenantsDeleteDialogProps) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const { mutateAsync: deleteTenant } = useDeleteTenant()
  const [isDeleting, setIsDeleting] = useState(false)

  const handleDelete = async () => {
    if (!currentRow) return
    setIsDeleting(true)
    try {
      await deleteTenant({ id: currentRow.id! })
      toast.success(t('Tenant deleted successfully'))
      queryClient.invalidateQueries({ queryKey: ['tenants'] })
      onOpenChange(false)
    } catch (error: any) {
      toast.error(error?.response?.data?.title || error?.message || t('Operation failed'))
    } finally {
      setIsDeleting(false)
    }
  }

  return (
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>{t('Are you sure you want to delete this tenant?')}</AlertDialogTitle>
          <AlertDialogDescription>
            {t('This action cannot be undone. This will permanently delete the tenant')} 
            <span className="font-bold text-foreground"> {currentRow?.name} </span> 
            {t('and remove all associated data.')}
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel>{t('Cancel')}</AlertDialogCancel>
          <AlertDialogAction 
            onClick={(e) => {
              e.preventDefault()
              handleDelete()
            }} 
            disabled={isDeleting}
            className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
          >
            {t('Delete')}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
