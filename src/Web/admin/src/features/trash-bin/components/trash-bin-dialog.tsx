import { AlertTriangle, RotateCcw } from 'lucide-react'
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
import type { TrashBinItemDto } from '@/api/model'

interface TrashBinActionDialogProps {
  target: { item: TrashBinItemDto; action: 'restore' | 'hardDelete' } | null
  loading: boolean
  onClose: () => void
  onConfirm: () => void
}

export function TrashBinActionDialog({
  target,
  loading,
  onClose,
  onConfirm,
}: TrashBinActionDialogProps) {
  const { t } = useTranslation()
  const isHardDelete = target?.action === 'hardDelete'

  return (
    <AlertDialog open={!!target} onOpenChange={(open) => !open && onClose()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle className='flex items-center gap-2'>
            {isHardDelete ? (
              <>
                <AlertTriangle className='h-5 w-5 text-destructive' />
                {t('Hard Delete Confirmation')}
              </>
            ) : (
              <>
                <RotateCcw className='h-5 w-5 text-emerald-600' />
                {t('Restore Confirmation')}
              </>
            )}
          </AlertDialogTitle>
          <AlertDialogDescription>
            {isHardDelete
              ? `${t('Are you sure you want to hard delete')} 【${target?.item.displayName || target?.item.id}】 ${t('This operation is irreversible and the data will be permanently wiped from database!')}`
              : `${t('Are you sure you want to restore')} 【${target?.item.displayName || target?.item.id}】 ${t('The data will reappear in system after restoration.')}`}
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={loading}>{t('Cancel')}</AlertDialogCancel>
          <AlertDialogAction
            onClick={onConfirm}
            disabled={loading}
            className={isHardDelete ? 'bg-destructive hover:bg-destructive/90' : 'bg-emerald-600 hover:bg-emerald-700'}
          >
            {loading ? t('Processing...') : isHardDelete ? t('Hard Delete') : t('Confirm Restore')}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
