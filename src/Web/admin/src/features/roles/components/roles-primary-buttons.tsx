import { useTranslation } from 'react-i18next'
import { Plus } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { useRolesContext } from './roles-provider'
import { usePermissions } from '@/hooks/use-permissions'

export function RolesPrimaryButtons() {
  const { setOpen } = useRolesContext()
  const { hasPermission } = usePermissions()
  const { t } = useTranslation()
  
  return (
    <div className='flex gap-2'>
      {hasPermission('Identity.Roles.Create') && (
        <Button className='space-x-1' onClick={() => setOpen('create')}>
          <span>{t('Create Role')}</span> <Plus size={18} />
        </Button>
      )}
    </div>
  )
}
