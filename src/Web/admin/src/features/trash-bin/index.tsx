import { useTranslation } from 'react-i18next'
import { Header } from '@/components/layout/header'
import { Main } from '@/components/layout/main'
import { ProfileDropdown } from '@/components/profile-dropdown'
import { Search } from '@/components/search'
import { ThemeSwitch } from '@/components/theme-switch'
import { TrashBinTable } from './components/trash-bin-table'

export default function TrashBinFeature() {
  const { t } = useTranslation()

  return (
    <>
      <Header fixed>
        <Search className='me-auto' />
        <ThemeSwitch />
        <ProfileDropdown />
      </Header>

      <Main className='flex flex-1 flex-col gap-4 sm:gap-6'>
        <div className='flex flex-wrap items-end justify-between gap-2'>
          <div>
            <h2 className='text-2xl font-bold tracking-tight'>{t('Data Recovery')}</h2>
            <p className='text-muted-foreground text-sm'>
              {t('View, restore, or permanently purge soft-deleted historical data in the system.')}
            </p>
          </div>
        </div>

        <TrashBinTable />
      </Main>
    </>
  )
}
