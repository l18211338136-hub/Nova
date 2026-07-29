import { Button } from '@/components/ui/button'
import { Plus } from 'lucide-react'
import { useTenantsContext } from './tenants-provider'
import { useTranslation } from 'react-i18next'
import { usePermissions } from '@/hooks/use-permissions'

export function TenantsPrimaryButtons() {
  const { setOpen } = useTenantsContext()
  const { t } = useTranslation()
  const { hasPermission } = usePermissions()
  return (
    <div className='flex gap-2'>
      {hasPermission('Multitenancy.Tenants.Create') && (
        <Button className='space-x-1' onClick={() => setOpen('create')}>
          <span>{t('Create Tenant')}</span> <Plus size={18} />
        </Button>
      )}
    </div>
  )
}
