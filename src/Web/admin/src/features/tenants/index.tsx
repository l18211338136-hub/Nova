import { useTranslation } from 'react-i18next'
import { Header } from '@/components/layout/header'
import { Main } from '@/components/layout/main'
import { ProfileDropdown } from '@/components/profile-dropdown'
import { Search } from '@/components/search'
import { ConfigDrawer } from '@/components/config-drawer'
import { ThemeSwitch } from '@/components/theme-switch'
import TenantsProvider from './components/tenants-provider'
import { TenantsTable } from './components/tenants-table'
import { TenantsDialogs } from './components/tenants-dialogs'
import { TenantsPrimaryButtons } from './components/tenants-primary-buttons'

export default function Tenants({ search, navigate }: { search: Record<string, unknown>, navigate: any }) {
  const { t } = useTranslation()
  return (
    <TenantsProvider>
      <Header fixed>
        <Search className='me-auto' />
        <ThemeSwitch />
        <ConfigDrawer />
        <ProfileDropdown />
      </Header>

      <Main className='flex flex-1 flex-col gap-4 sm:gap-6'>
        <div className='flex flex-wrap items-end justify-between gap-2'>
          <div>
            <h2 className='text-2xl font-bold tracking-tight'>{t('Tenants')}</h2>
            <p className='text-muted-foreground'>
              {t('Manage system tenants here.')}
            </p>
          </div>
          <TenantsPrimaryButtons />
        </div>
        <TenantsTable search={search} navigate={navigate} />
      </Main>

      <TenantsDialogs />
    </TenantsProvider>
  )
}
