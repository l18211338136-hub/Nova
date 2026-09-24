import { getRouteApi } from '@tanstack/react-router'
import { useTranslation } from 'react-i18next'
import { Header } from '@/components/layout/header'
import { Main } from '@/components/layout/main'
import { ProfileDropdown } from '@/components/profile-dropdown'
import { Search } from '@/components/search'
import { ThemeSwitch } from '@/components/theme-switch'
import { McpKeysDialogs } from './components/mcp-keys-dialogs'
import { McpKeysProvider } from './components/mcp-keys-provider'
import { McpKeysTable } from './components/mcp-keys-table'

const route = getRouteApi('/_authenticated/mcp-keys/')

export function McpKeys() {
  const { t } = useTranslation()
  const search = route.useSearch()
  const navigate = route.useNavigate()

  return (
    <McpKeysProvider>
      <Header fixed>
        <Search className='me-auto' />
        <ThemeSwitch />
        <ProfileDropdown />
      </Header>

      <Main className='flex flex-1 flex-col gap-4 sm:gap-6'>
        <div className='flex flex-wrap items-end justify-between gap-2'>
          <div>
            <h2 className='text-2xl font-bold tracking-tight'>{t('MCP Keys')}</h2>
            <p className='text-muted-foreground'>
              {t('Manage your MCP access keys here.')}
            </p>
          </div>
        </div>
        <McpKeysTable search={search} navigate={navigate} />
      </Main>

      <McpKeysDialogs />
    </McpKeysProvider>
  )
}
