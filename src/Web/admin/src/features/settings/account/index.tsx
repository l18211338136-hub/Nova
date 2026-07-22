import { ContentSection } from '../components/content-section'
import { useTranslation } from 'react-i18next'
import { AccountForm } from './account-form'

export function SettingsAccount() {
  const { t } = useTranslation()
  return (
    <ContentSection
      title={t('Account')}
      desc={t('Update your account settings. Set your preferred language and timezone.')}
    >
      <AccountForm />
    </ContentSection>
  )
}
