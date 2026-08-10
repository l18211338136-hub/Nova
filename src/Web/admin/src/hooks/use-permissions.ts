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

    const userPermissions = user.Permission || user.permission || []
    
    // 如果 token 里只有单个权限字符串，转换成数组
    const normalizedPermissions: string[] = Array.isArray(userPermissions) 
      ? userPermissions 
      : [userPermissions]

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
