import { useLayout } from '@/context/layout-provider'
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarHeader,
  SidebarRail,
} from '@/components/ui/sidebar'
// import { AppTitle } from './app-title'
import { sidebarData } from './data/sidebar-data'
import { NavGroup } from './nav-group'
import { NavUser } from './nav-user'
import { TeamSwitcher } from './team-switcher'
import { useAuthStore } from '@/stores/auth-store'
import { useSidebarNav } from '@/hooks/use-sidebar-nav'
import { useGetProfile } from '@/api/endpoints/profile'

export function AppSidebar() {
  const { collapsible, variant } = useLayout()
  const { user } = useAuthStore((state) => state.auth)

  // 侧边栏的分组已在 useSidebarNav 里按用户的「显示」偏好过滤过
  const { visibleGroups } = useSidebarNav()

  const { data: profileResponse } = useGetProfile({
    query: { enabled: Boolean(user), staleTime: 5 * 60 * 1000 },
  })
  const profile = profileResponse?.data

  const nameClaim =
    user?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] ||
    user?.name ||
    'User'
  const fallbackEmail = nameClaim.includes('@')
    ? nameClaim
    : `${nameClaim}@nova.com`

  const activeUser = user
    ? {
        name: profile?.nickName || profile?.userName || nameClaim,
        email: profile?.email || fallbackEmail,
        avatar: profile?.avatarUrl || '/avatars/shadcn.jpg',
      }
    : sidebarData.user

  return (
    <Sidebar collapsible={collapsible} variant={variant}>
      <SidebarHeader>
        <TeamSwitcher teams={sidebarData.teams} />

        {/* Replace <TeamSwitch /> with the following <AppTitle />
         /* if you want to use the normal app title instead of TeamSwitch dropdown */}
        {/* <AppTitle /> */}
      </SidebarHeader>
      <SidebarContent>
        {visibleGroups.map((props) => (
          <NavGroup key={props.title} {...props} />
        ))}
      </SidebarContent>
      <SidebarFooter>
        <NavUser user={activeUser} />
      </SidebarFooter>
      <SidebarRail />
    </Sidebar>
  )
}
