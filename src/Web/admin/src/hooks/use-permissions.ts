import { useAuthStore } from '@/stores/auth-store'

export function usePermissions() {
  const { user } = useAuthStore((state) => state.auth)

  const hasPermission = (permission: string | string[]) => {
    if (!user) return false

    const userPermissions = user.Permission || user.permission || []
    
    // 如果 token 里只有单个权限字符串，转换成数组
    const normalizedPermissions = Array.isArray(userPermissions) 
      ? userPermissions 
      : [userPermissions]

    // 如果拥有通配符 '*'，则代表拥有所有权限，直接放行
    if (normalizedPermissions.includes('*')) {
      return true
    }

    if (Array.isArray(permission)) {
      // 要求拥有其中任意一个权限即可
      return permission.some((p) => normalizedPermissions.includes(p))
    }

    return normalizedPermissions.includes(permission)
  }

  return { hasPermission }
}
