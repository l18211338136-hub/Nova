import { ConfigDrawer } from '@/components/config-drawer'
import { Header } from '@/components/layout/header'
import { Main } from '@/components/layout/main'
import { ProfileDropdown } from '@/components/profile-dropdown'
import { Search } from '@/components/search'
import { ThemeSwitch } from '@/components/theme-switch'
import MenusProvider from './components/menus-provider'
import { MenusTable } from './components/menus-table'
import { MenusDialogs } from './components/menus-dialogs'
import { MenusPrimaryButtons } from './components/menus-primary-buttons'
import { useTranslation } from 'react-i18next'

export default function Menus({ search: _search, navigate: _navigate }: { search: Record<string, unknown>, navigate: any }) {
  const { t } = useTranslation()
  return (
    <MenusProvider>
      <Header fixed>
        <Search className='me-auto' />
        <ThemeSwitch />
        <ConfigDrawer />
        <ProfileDropdown />
      </Header>

      <Main className='flex flex-1 flex-col gap-4 sm:gap-6'>
        <div className='flex flex-wrap items-end justify-between gap-2'>
          <div>
            <h2 className='text-2xl font-bold tracking-tight'>{t('Menus')}</h2>
            <p className='text-muted-foreground'>
              {t('Manage your system menus and routes here.')}
            </p>
          </div>
          <MenusPrimaryButtons />
        </div>
        <MenusTable search={_search} navigate={_navigate} />
      </Main>

      <MenusDialogs />
    </MenusProvider>
  )
}
