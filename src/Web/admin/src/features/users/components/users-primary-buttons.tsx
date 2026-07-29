import { MailPlus, UserPlus } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { usePermissions } from '@/hooks/use-permissions'
import { useUsersContext } from './users-provider'

export function UsersPrimaryButtons() {
  const { hasPermission } = usePermissions()
  const { setOpen } = useUsersContext()
  const { t } = useTranslation()
  return (
    <div className='flex gap-2'>
      {hasPermission('Identity.Users.Create') && (
        <>
          <Button
            variant='outline'
            className='space-x-1'
            onClick={() => setOpen('invite')}
          >
            <span>{t('Invite User')}</span> <MailPlus size={18} />
          </Button>
          <Button className='space-x-1' onClick={() => setOpen('add')}>
            <span>{t('Add User')}</span> <UserPlus size={18} />
          </Button>
        </>
      )}
    </div>
  )
}
