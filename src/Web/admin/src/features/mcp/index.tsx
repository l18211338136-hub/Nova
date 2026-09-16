import { getRouteApi } from '@tanstack/react-router'
import { useTranslation } from 'react-i18next'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Header } from '@/components/layout/header'
import { Main } from '@/components/layout/main'
import { ProfileDropdown } from '@/components/profile-dropdown'
import { Search } from '@/components/search'
import { ThemeSwitch } from '@/components/theme-switch'
import { McpProvider } from './components/mcp-provider'
import { McpPrimaryButtons } from './components/mcp-primary-buttons'
import { McpServersTable } from './components/mcp-servers-table'
import { McpToolsTable } from './components/mcp-tools-table'
import { McpImportDialog } from './components/mcp-import-dialog'

const route = getRouteApi('/_authenticated/mcp/')

export function Mcp() {
  const { t } = useTranslation()
  const search = route.useSearch()
  const navigate = route.useNavigate()

  return (
    <McpProvider>
      <Header fixed>
        <Search className='me-auto' />
        <ThemeSwitch />
        <ProfileDropdown />
      </Header>

      <Main className='flex flex-1 flex-col gap-4 sm:gap-6'>
        <div className='flex flex-wrap items-end justify-between gap-2'>
          <div>
            <h2 className='text-2xl font-bold tracking-tight'>
              {t('MCP Servers')}
            </h2>
            <p className='text-muted-foreground'>
              {t('Manage your MCP servers and tools here.')}
            </p>
          </div>
          <McpPrimaryButtons />
        </div>

        <Tabs defaultValue='servers' className='flex flex-1 flex-col gap-4'>
          <TabsList>
            <TabsTrigger value='servers'>{t('MCP Servers')}</TabsTrigger>
            <TabsTrigger value='tools'>{t('MCP Tools')}</TabsTrigger>
          </TabsList>
          <TabsContent value='servers' className='flex flex-1 flex-col'>
            <McpServersTable search={search} navigate={navigate} />
          </TabsContent>
          <TabsContent value='tools' className='flex flex-1 flex-col'>
            <McpToolsTable search={search} navigate={navigate} />
          </TabsContent>
        </Tabs>
      </Main>

      <McpImportDialog />
    </McpProvider>
  )
}
