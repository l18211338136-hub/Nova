import { toast } from 'sonner'
import { AlertTriangle } from 'lucide-react'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { MenuDto } from './menus-provider'
import { useTranslation } from 'react-i18next'
import { useQueryClient } from '@tanstack/react-query'
import { useDeleteMenu, getMenusQueryKey } from '@/api/endpoints/menus'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  currentRow?: MenuDto | null
}

export function MenusDeleteDialog({ open, onOpenChange, currentRow }: Props) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const deleteMenuMutation = useDeleteMenu()

  const onDelete = () => {
    if (!currentRow) return
    deleteMenuMutation.mutate(
      { id: currentRow.id! },
      {
        onSuccess: () => {
          queryClient.invalidateQueries({ queryKey: ['menus'] })
          toast.success(t('Menu deleted successfully'))
          onOpenChange(false)
        },
        onError: (error: any) => {
          toast.error(error.response?.data?.message || t('Failed to delete menu'))
        }
      }
    )
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <AlertTriangle className="h-5 w-5 text-red-500" />
            {t('Delete Menu')}
          </DialogTitle>
          <DialogDescription>
            {t('Are you sure you want to delete this menu?')}{' '}
            <span className="font-semibold text-foreground">
              {currentRow?.name}
            </span>
            ? {t('This action cannot be undone.')}
          </DialogDescription>
        </DialogHeader>
        <DialogFooter className="gap-2 sm:gap-0">
          <Button
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={deleteMenuMutation.isPending}
          >
            {t('Cancel')}
          </Button>
          <Button 
            variant="destructive" 
            onClick={onDelete}
            disabled={deleteMenuMutation.isPending}
          >
            {deleteMenuMutation.isPending ? t('Deleting...') : t('Delete')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
