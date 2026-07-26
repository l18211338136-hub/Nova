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
import { useMenus } from '@/api/endpoints/menus'
import * as Icons from 'lucide-react'
import { useMemo } from 'react'

export function AppSidebar() {
  const { collapsible, variant } = useLayout()
  const { user } = useAuthStore((state) => state.auth)

  const nameClaim = user?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] || user?.name || 'User'
  const email = nameClaim.includes('@') ? nameClaim : `${nameClaim}@nova.com`
  
  const activeUser = user ? {
    name: nameClaim,
    email: email,
    avatar: '/avatars/shadcn.jpg',
  } : sidebarData.user

  const { data: apiResponse } = useMenus({
    query: {
      queryKey: ['sidebar-menus'],
    },
    request: {
      params: {
        $count: true,
        $top: 1000,
        $orderby: 'Sort asc',
        $filter: 'IsEnabled eq true'
      }
    }
  })

  const dynamicGroups = useMemo(() => {
    const rawMenus = (apiResponse?.data?.items || []) as any[]
    const map = new Map<string, any>()
    const roots: any[] = []

    rawMenus.forEach(item => {
      if (item.id) map.set(item.id, { ...item, subRows: [] })
    })

    rawMenus.forEach(item => {
      if (!item.id) return
      const node = map.get(item.id)!
      if (item.parentId && map.has(item.parentId)) {
        map.get(item.parentId)!.subRows.push(node)
      } else {
        roots.push(node)
      }
    })

    const sortNodes = (nodes: any[]) => {
      nodes.sort((a, b) => (a.sort || 0) - (b.sort || 0))
      nodes.forEach(n => {
        if (n.subRows && n.subRows.length > 0) sortNodes(n.subRows)
      })
    }
    sortNodes(roots)

    const mapToNavItems = (nodes: any[]): any[] => {
      return nodes.map(node => {
        const IconComponent = node.icon && (Icons as any)[node.icon]
        return {
          title: node.name,
          url: node.path,
          icon: IconComponent || Icons.Circle,
          items: node.subRows?.length ? mapToNavItems(node.subRows) : undefined
        }
      })
    }

    return roots.map(root => ({
      title: root.name,
      items: mapToNavItems(root.subRows)
    }))
  }, [apiResponse?.data?.items])

  const allNavGroups = [...dynamicGroups] // ...sidebarData.navGroups (Hidden for now as requested)

  return (
    <Sidebar collapsible={collapsible} variant={variant}>
      <SidebarHeader>
        <TeamSwitcher teams={sidebarData.teams} />

        {/* Replace <TeamSwitch /> with the following <AppTitle />
         /* if you want to use the normal app title instead of TeamSwitch dropdown */}
        {/* <AppTitle /> */}
      </SidebarHeader>
      <SidebarContent>
        {allNavGroups.map((props) => (
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
