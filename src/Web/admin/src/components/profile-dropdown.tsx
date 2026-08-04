import { Link } from '@tanstack/react-router'
import { useTranslation } from 'react-i18next'
import useDialogState from '@/hooks/use-dialog-state'
import { useAuthStore } from '@/stores/auth-store'
import { useGetProfile } from '@/api/endpoints/profile'
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar'
import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuShortcut,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { Badge } from '@/components/ui/badge'
import { SignOutDialog } from '@/components/sign-out-dialog'

function getInitials(name: string) {
  return name
    .split(/\s+/)
    .slice(0, 2)
    .map((part) => part[0])
    .join('')
    .toUpperCase()
}

export function ProfileDropdown() {
  const [open, setOpen] = useDialogState()
  const { t } = useTranslation()
  const { user, accessToken } = useAuthStore((state) => state.auth)

  const { data: profileResponse, isLoading } = useGetProfile({
    query: {
      enabled: Boolean(accessToken),
      staleTime: 5 * 60 * 1000,
      refetchOnWindowFocus: false,
    },
  })
  const profile = profileResponse?.data

  // Fallback to token claims while profile loads
  const nameClaim =
    user?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] ||
    user?.name ||
    'User'
  const fallbackEmail = nameClaim.includes('@')
    ? nameClaim
    : `${nameClaim}@nova.com`

  const displayName = profile?.nickName || profile?.userName || nameClaim
  const email = profile?.email || fallbackEmail
  const avatarUrl = profile?.avatarUrl
  const initials = getInitials(displayName).substring(0, 2)

  return (
    <>
      <DropdownMenu modal={false}>
        <DropdownMenuTrigger asChild>
          <Button variant='ghost' className='relative h-8 w-8 rounded-full'>
            <Avatar className='h-8 w-8'>
              {avatarUrl ? (
                <AvatarImage src={avatarUrl} alt={displayName} />
              ) : null}
              <AvatarFallback>{isLoading ? '…' : initials}</AvatarFallback>
            </Avatar>
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent className='w-56' align='end' forceMount>
          <DropdownMenuLabel className='font-normal'>
            <div className='flex flex-col gap-1.5'>
              <p className='text-sm leading-none font-medium'>{displayName}</p>
              <p className='text-xs leading-none text-muted-foreground'>
                {email}
              </p>
              {profile?.roles && profile.roles.length > 0 && (
                <div className='flex flex-wrap gap-1 pt-0.5'>
                  {profile.roles.map((role) => (
                    <Badge
                      key={role.name}
                      variant='secondary'
                      className='px-1 py-0 text-[10px]'
                    >
                      {role.displayName || role.name}
                    </Badge>
                  ))}
                </div>
              )}
            </div>
          </DropdownMenuLabel>
          <DropdownMenuSeparator />
          <DropdownMenuGroup>
            <DropdownMenuItem asChild>
              <Link to='/settings'>
                {t('Profile')}
                <DropdownMenuShortcut>⇧⌘P</DropdownMenuShortcut>
              </Link>
            </DropdownMenuItem>
            <DropdownMenuItem asChild>
              <Link to='/settings'>
                {t('Billing')}
                <DropdownMenuShortcut>⌘B</DropdownMenuShortcut>
              </Link>
            </DropdownMenuItem>
            <DropdownMenuItem asChild>
              <Link to='/settings'>
                {t('Settings')}
                <DropdownMenuShortcut>⌘S</DropdownMenuShortcut>
              </Link>
            </DropdownMenuItem>
            <DropdownMenuItem>{t('New Team')}</DropdownMenuItem>
          </DropdownMenuGroup>
          <DropdownMenuSeparator />
          <DropdownMenuItem variant='destructive' onClick={() => setOpen(true)}>
            {t('Sign out')}
            <DropdownMenuShortcut className='text-current'>
              ⇧⌘Q
            </DropdownMenuShortcut>
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>

      <SignOutDialog open={!!open} onOpenChange={setOpen} />
    </>
  )
}
