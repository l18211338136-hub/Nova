import { getRouteApi } from '@tanstack/react-router'
import { useTranslation } from 'react-i18next'
import { Header } from '@/components/layout/header'
import { Main } from '@/components/layout/main'
import { ProfileDropdown } from '@/components/profile-dropdown'
import { Search } from '@/components/search'
import { ThemeSwitch } from '@/components/theme-switch'
import { McpProvider } from './components/mcp-provider'
import { McpPrimaryButtons } from './components/mcp-primary-buttons'
import { McpServersTable } from './components/mcp-servers-table'
import { McpDialogs } from './components/mcp-dialogs'

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
            <p className='text-muted-foreground text-sm'>
              {t('Manage your MCP servers and tools here.')}
            </p>
          </div>
        </div>

        {/*
         * 「MCP 工具」tab 已下线：工具列表改为服务行操作里的「详情」，
         * 按分组 / 分项只读展示（对齐导入第 2 步的分组渲染）。
         * pr-24 为右侧绝对定位的「视图」按钮预留空间，保证同一水平线不重叠。
         */}
        <div className='relative flex flex-1 flex-col gap-4 min-w-0 w-full overflow-hidden'>
          <div className='flex items-center justify-end gap-2 pr-24'>
            <McpPrimaryButtons />
          </div>
          <McpServersTable search={search} navigate={navigate} />
        </div>
      </Main>

      <McpDialogs />
    </McpProvider>
  )
}
