import { useNavigate, useLocation } from '@tanstack/react-router'
import { useTranslation } from 'react-i18next'
import { useAuthStore } from '@/stores/auth-store'
import { useLogout } from '@/api/endpoints/auth'
import { ConfirmDialog } from '@/components/confirm-dialog'

interface SignOutDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function SignOutDialog({ open, onOpenChange }: SignOutDialogProps) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const location = useLocation()
  const { auth } = useAuthStore()
  const logoutMutation = useLogout()

  const handleSignOut = () => {
    // Revoke the refresh token on the server, then clear local state.
    logoutMutation.mutate(
      {
        data: {
          accessToken: auth.accessToken,
          refreshToken: auth.refreshToken,
        },
      },
      {
        onSettled: () => {
          auth.reset()
          // Preserve current location for redirect after sign-in
          const currentPath = location.href
          navigate({
            to: '/sign-in',
            search: { redirect: currentPath },
            replace: true,
          })
        },
      }
    )
  }

  return (
    <ConfirmDialog
      open={open}
      onOpenChange={onOpenChange}
      title={t('Sign out')}
      desc={t('Are you sure you want to sign out? You will need to sign in again to access your account.')}
      confirmText={t('Sign out')}
      destructive
      handleConfirm={handleSignOut}
      className='sm:max-w-sm'
    />
  )
}
