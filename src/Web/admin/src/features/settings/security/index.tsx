import { ContentSection } from '../components/content-section'
import { useTranslation } from 'react-i18next'
import { SecurityForm } from './security-form'

export function SettingsSecurity() {
  const { t } = useTranslation()
  return (
    <ContentSection
      title={t('Account security')}
      desc={t(
        'Change your password. You will need your current password to set a new one.'
      )}
    >
      <SecurityForm />
    </ContentSection>
  )
}
