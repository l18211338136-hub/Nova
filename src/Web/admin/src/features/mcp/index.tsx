import { useState } from 'react'
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
import { McpDeleteDialog } from './components/mcp-delete-dialog'

const route = getRouteApi('/_authenticated/mcp/')

export function Mcp() {
  const { t } = useTranslation()
  const search = route.useSearch()
  const navigate = route.useNavigate()
  const [tab, setTab] = useState('servers')

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
            <p className='text-muted-foreground text-sm'>
              {t('Manage your MCP servers and tools here.')}
            </p>
          </div>
        </div>

        <Tabs
          value={tab}
          onValueChange={setTab}
          className='flex flex-1 flex-col gap-4 min-w-0 w-full overflow-hidden relative'
        >
          <div className='flex items-center justify-between gap-2'>
            <TabsList className='grid w-full max-w-[240px] grid-cols-2 h-10 p-1'>
              <TabsTrigger value='servers' className='px-3 text-xs sm:text-sm font-medium'>
                {t('MCP Servers')}
              </TabsTrigger>
              <TabsTrigger value='tools' className='px-3 text-xs sm:text-sm font-medium'>
                {t('MCP Tools')}
              </TabsTrigger>
            </TabsList>
            {/* pr-24 为右侧绝对定位的「视图」按钮预留空间，保证同一水平线不重叠 */}
            <div className='flex items-center gap-2 pr-24'>
              {tab === 'servers' && <McpPrimaryButtons />}
            </div>
          </div>
          <TabsContent value='servers' className='flex-1 flex flex-col m-0 min-w-0 w-full overflow-hidden outline-none'>
            <McpServersTable search={search} navigate={navigate} />
          </TabsContent>
          <TabsContent value='tools' className='flex-1 flex flex-col m-0 min-w-0 w-full overflow-hidden outline-none'>
            <McpToolsTable search={search} navigate={navigate} />
          </TabsContent>
        </Tabs>
      </Main>

      <McpImportDialog />
      <McpDeleteDialog kind='server' />
      <McpDeleteDialog kind='tool' />
    </McpProvider>
  )
}
