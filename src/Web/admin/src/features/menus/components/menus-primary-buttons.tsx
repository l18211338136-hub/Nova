import { Button } from '@/components/ui/button'
import { Plus } from 'lucide-react'
import { useMenusContext } from './menus-provider'
import { useTranslation } from 'react-i18next'
import { usePermissions } from '@/hooks/use-permissions'

export function MenusPrimaryButtons() {
  const { setOpen, setCurrentRow } = useMenusContext()
  const { t } = useTranslation()
  const { hasPermission } = usePermissions()

  if (!hasPermission('Identity.Menus.Create')) {
    return null
  }

  return (
    <div className='flex gap-2'>
      <Button
        className='space-x-1'
        onClick={() => {
          setCurrentRow(null)
          setOpen('create')
        }}
      >
        <span>{t('Create Menu')}</span>
        <Plus size={18} />
      </Button>
    </div>
  )
}
