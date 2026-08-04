import { useMemo } from 'react'
import * as Icons from 'lucide-react'
import { useGetMyMenus } from '@/api/endpoints/menus'
import { useAuthStore } from '@/stores/auth-store'
import type { NavGroup, NavItem } from '@/components/layout/types'
import { usePreferences } from '@/hooks/use-preferences'

type RawMenu = {
  id?: string
  parentId?: string | null
  name?: string
  path?: string
  icon?: string
  sort?: number
}

type MenuNode = RawMenu & { children: MenuNode[] }

/** 侧边栏中可被用户隐藏的一个叶子入口。 */
export type SidebarNavOption = {
  /** 菜单路径，同时作为隐藏配置里的唯一标识 */
  url: string
  /** 展示名称 */
  title: string
  /** 所属分组名，用于在设置页里分组罗列 */
  group: string
}

function buildTree(rawMenus: RawMenu[]): MenuNode[] {
  const map = new Map<string, MenuNode>()
  const roots: MenuNode[] = []

  rawMenus.forEach((item) => {
    if (item.id) map.set(item.id, { ...item, children: [] })
  })

  rawMenus.forEach((item) => {
    if (!item.id) return
    const node = map.get(item.id)
    if (!node) return

    if (item.parentId && map.has(item.parentId)) {
      map.get(item.parentId)!.children.push(node)
    } else {
      roots.push(node)
    }
  })

  const sortNodes = (nodes: MenuNode[]) => {
    nodes.sort((a, b) => (a.sort || 0) - (b.sort || 0))
    nodes.forEach((n) => sortNodes(n.children))
  }
  sortNodes(roots)

  return roots
}

function toNavItems(nodes: MenuNode[]): NavItem[] {
  return nodes.map((node) => {
    const IconComponent =
      node.icon && (Icons as unknown as Record<string, React.ElementType>)[node.icon]
    const children = node.children.length ? toNavItems(node.children) : undefined

    return {
      title: node.name ?? '',
      url: node.path ?? '',
      icon: IconComponent || Icons.Circle,
      items: children,
    } as NavItem
  })
}

/** 从导航项里收集所有可点击的叶子链接（折叠分组本身不算入口）。 */
function collectOptions(items: NavItem[], group: string): SidebarNavOption[] {
  const result: SidebarNavOption[] = []

  const walk = (list: NavItem[]) => {
    list.forEach((item) => {
      if (item.items?.length) {
        walk(item.items as NavItem[])
        return
      }
      if (!item.url) return
      result.push({ url: String(item.url), title: item.title, group })
    })
  }

  walk(items)
  return result
}

/** 按隐藏列表过滤导航树；分组下的入口全被隐藏时，整个分组也不再渲染。 */
function applyHidden(groups: NavGroup[], hidden: Set<string>): NavGroup[] {
  const filterItems = (items: NavItem[]): NavItem[] =>
    items.reduce<NavItem[]>((acc, item) => {
      if (item.items?.length) {
        const kept = filterItems(item.items as NavItem[])
        if (kept.length) acc.push({ ...item, items: kept } as NavItem)
        return acc
      }
      if (item.url && hidden.has(String(item.url))) return acc
      acc.push(item)
      return acc
    }, [])

  return groups
    .map((group) => ({ ...group, items: filterItems(group.items) }))
    .filter((group) => group.items.length > 0)
}

/**
 * 统一的侧边栏导航数据源。
 *
 * 侧边栏本体与「设置 → 显示」页共用同一份菜单，
 * 这样设置页里勾选的入口一定和真实渲染的入口一一对应。
 */
export function useSidebarNav() {
  const { user } = useAuthStore((state) => state.auth)
  const { preferences, isLoading: isPreferencesLoading } = usePreferences()

  const { data: apiResponse, isLoading: isMenusLoading } = useGetMyMenus({
    query: {
      enabled: Boolean(user),
      staleTime: 30 * 1000,
      refetchOnWindowFocus: true,
    },
  })

  const allGroups = useMemo<NavGroup[]>(() => {
    const rawMenus = (apiResponse?.data ?? []) as RawMenu[]
    const roots = buildTree(rawMenus)

    // 后端菜单的一级节点当作分组标题，其子节点才是真正的入口
    const dynamicGroups: NavGroup[] = roots.map((root) => ({
      title: root.name ?? '',
      items: toNavItems(root.children),
    }))

    return dynamicGroups
  }, [apiResponse?.data])

  const options = useMemo<SidebarNavOption[]>(
    () => allGroups.flatMap((group) => collectOptions(group.items, group.title)),
    [allGroups]
  )

  const hiddenItems = useMemo(
    () => new Set(preferences.hiddenSidebarItems ?? []),
    [preferences.hiddenSidebarItems]
  )

  const visibleGroups = useMemo(
    () => applyHidden(allGroups, hiddenItems),
    [allGroups, hiddenItems]
  )

  return {
    /** 未经隐藏过滤的完整分组，供设置页罗列全部可选项 */
    allGroups,
    /** 应用隐藏配置后的分组，供侧边栏渲染 */
    visibleGroups,
    /** 扁平化的可选入口列表 */
    options,
    /** 当前被隐藏的入口 url 集合 */
    hiddenItems,
    isLoading: isMenusLoading || isPreferencesLoading,
  }
}
