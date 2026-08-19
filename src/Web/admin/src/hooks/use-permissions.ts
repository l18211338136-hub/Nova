import { useAuthStore } from '@/stores/auth-store'

const isMatch = (userPerm: string, targetPerm: string): boolean => {
  if (!userPerm || !targetPerm) return false
  const uPerm = userPerm.trim()
  const tPerm = targetPerm.trim()

  // 1. 精确匹配或全局通配符 *
  if (uPerm === '*' || uPerm === tPerm) return true

  // 2. 层级/前缀通配符匹配（例如 Identity.Users.* 或 Identity.*）
  if (uPerm.endsWith('.*')) {
    const prefix = uPerm.slice(0, -1) // 保留结尾点号 "Identity.Users."
    if (tPerm.startsWith(prefix)) return true
  }

  return false
}

export function usePermissions() {
  const { user } = useAuthStore((state) => state.auth)

  const hasPermission = (permission: string | string[]) => {
    if (!user) return false

    // 1. 超级管理员 / root 角色判定：超级管理员拥有全量权限
    const roles =
      user['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ||
      user.role ||
      user.roles ||
      []
    const normalizedRoles = Array.isArray(roles) ? roles : [roles]
    const isSuperUser = normalizedRoles.some(
      (r) =>
        typeof r === 'string' &&
        ['root', 'superadmin', 'admin'].includes(r.trim().toLowerCase())
    )
    if (isSuperUser) return true

    // 2. 提取并标准化用户权限列表
    const rawPermissions =
      user.Permission || user.permission || user.permissions || []
    const normalizedPermissions: string[] = Array.isArray(rawPermissions)
      ? rawPermissions
      : [rawPermissions]

    if (normalizedPermissions.includes('*')) return true

    if (Array.isArray(permission)) {
      // 要求拥有其中任意一个权限即可
      return permission.some((p) =>
        normalizedPermissions.some((uPerm) => isMatch(uPerm, p))
      )
    }

    return normalizedPermissions.some((uPerm) => isMatch(uPerm, permission))
  }

  return { hasPermission }
}
