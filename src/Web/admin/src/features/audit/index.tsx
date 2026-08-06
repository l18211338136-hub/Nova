import { useState } from 'react'
import { getRouteApi } from '@tanstack/react-router'
import { useTranslation } from 'react-i18next'
import { ShieldCheck, List, History } from 'lucide-react'
import { ConfigDrawer } from '@/components/config-drawer'
import { Header } from '@/components/layout/header'
import { Main } from '@/components/layout/main'
import { ProfileDropdown } from '@/components/profile-dropdown'
import { Search } from '@/components/search'
import { ThemeSwitch } from '@/components/theme-switch'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { AuditTable } from './components/audit-table'
import { OperationTable } from './components/operation-table'
import { EntityDiffTable } from './components/entity-diff-table'

const route = getRouteApi('/_authenticated/audit/')

export function AuditLogs() {
  const { t } = useTranslation()
  const search = route.useSearch()
  const navigate = route.useNavigate()
  const [tab, setTab] = useState<'security' | 'operation' | 'entityDiff'>('operation')

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
              {t('审计与操作日志')}
            </h2>
            <p className='text-muted-foreground text-sm'>
              {t('记录系统全量 HTTP 操作日志、字段级数据变更 Diff 追溯、敏感词脱敏明细及安全认证审计轨迹。')}
            </p>
          </div>
        </div>

        <Tabs value={tab} onValueChange={(v) => setTab(v as 'security' | 'operation' | 'entityDiff')} className='flex flex-1 flex-col gap-4'>
          <TabsList className='w-fit grid grid-cols-3'>
            <TabsTrigger value='operation' className='gap-2 px-4'>
              <List className='h-4 w-4' />
              {t('全局操作日志')}
            </TabsTrigger>
            <TabsTrigger value='entityDiff' className='gap-2 px-4'>
              <History className='h-4 w-4' />
              {t('数据变更追溯')}
            </TabsTrigger>
            <TabsTrigger value='security' className='gap-2 px-4'>
              <ShieldCheck className='h-4 w-4' />
              {t('安全认证日志')}
            </TabsTrigger>
          </TabsList>

          <TabsContent value='operation' className='flex-1 flex flex-col m-0'>
            <OperationTable search={search} navigate={navigate} />
          </TabsContent>

          <TabsContent value='entityDiff' className='flex-1 flex flex-col m-0'>
            <EntityDiffTable search={search} navigate={navigate} />
          </TabsContent>

          <TabsContent value='security' className='flex-1 flex flex-col m-0'>
            <AuditTable search={search} navigate={navigate} />
          </TabsContent>
        </Tabs>
      </Main>
    </>
  )
}
