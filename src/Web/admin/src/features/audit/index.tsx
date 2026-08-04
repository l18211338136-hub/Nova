import { getRouteApi } from '@tanstack/react-router'
import { useTranslation } from 'react-i18next'
import { ConfigDrawer } from '@/components/config-drawer'
import { Header } from '@/components/layout/header'
import { Main } from '@/components/layout/main'
import { ProfileDropdown } from '@/components/profile-dropdown'
import { Search } from '@/components/search'
import { ThemeSwitch } from '@/components/theme-switch'
import { AuditTable } from './components/audit-table'

const route = getRouteApi('/_authenticated/audit/')

export function AuditLogs() {
  const { t } = useTranslation()
  const search = route.useSearch()
  const navigate = route.useNavigate()

  return (
    <>
      <Header fixed>
        <Search className='me-auto' />
        <ThemeSwitch />
        <ConfigDrawer />
        <ProfileDropdown />
      </Header>

      <Main className='flex flex-1 flex-col gap-4 sm:gap-6'>
        <div className='flex flex-wrap items-end justify-between gap-2'>
          <div>
            <h2 className='text-2xl font-bold tracking-tight'>
              {t('Audit Logs')}
            </h2>
            <p className='text-muted-foreground'>
              {t(
                'Security audit trail: logins, token refreshes, password changes and sign-outs.'
              )}
            </p>
          </div>
        </div>
        <AuditTable search={search} navigate={navigate} />
      </Main>
    </>
  )
}
