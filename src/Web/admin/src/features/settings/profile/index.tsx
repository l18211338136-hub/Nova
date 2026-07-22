import { ContentSection } from '../components/content-section'
import { useTranslation } from 'react-i18next'
import { ProfileForm } from './profile-form'

export function SettingsProfile() {
  const { t } = useTranslation()
  return (
    <ContentSection
      title={t('Profile')}
      desc={t('This is how others will see you on the site.')}
    >
      <ProfileForm />
    </ContentSection>
  )
}
