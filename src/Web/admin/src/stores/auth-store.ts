import { create } from 'zustand'
import { getCookie, setCookie, removeCookie } from '@/lib/cookies'
import { jwtDecode } from 'jwt-decode'

const ACCESS_TOKEN = 'access_token'
const REFRESH_TOKEN = 'refresh_token'

interface AuthUser {
  accountNo?: string
  email?: string
  role?: string[]
  exp?: number
  [key: string]: any
}

interface AuthState {
  auth: {
    user: AuthUser | null
    setUser: (user: AuthUser | null) => void
    accessToken: string
    setAccessToken: (accessToken: string) => void
    resetAccessToken: () => void
    refreshToken: string
    setRefreshToken: (refreshToken: string) => void
    reset: () => void
  }
}

export const useAuthStore = create<AuthState>()((set) => {
  const safeParse = (str: string | undefined): string => {
    if (!str) return ''
    try {
      return JSON.parse(str)
    } catch {
      return str
    }
  }

  const cookieAccess = getCookie(ACCESS_TOKEN)
  const cookieRefresh = getCookie(REFRESH_TOKEN)
  const initAccessToken = safeParse(cookieAccess)
  const initRefreshToken = safeParse(cookieRefresh)

  let initUser = null
  if (initAccessToken) {
    try {
      initUser = jwtDecode<AuthUser>(initAccessToken)
    } catch (e) {
      // token invalid
    }
  }

  return {
    auth: {
      user: initUser,
      setUser: (user) =>
        set((state) => ({ ...state, auth: { ...state.auth, user } })),
      
      accessToken: initAccessToken,
      setAccessToken: (accessToken) =>
        set((state) => {
          setCookie(ACCESS_TOKEN, JSON.stringify(accessToken))
          let user: AuthUser | null = null
          try {
            if (accessToken) user = jwtDecode<AuthUser>(accessToken)
          } catch (e) {}
          return { ...state, auth: { ...state.auth, accessToken, user } }
        }),
        
      resetAccessToken: () =>
        set((state) => {
          removeCookie(ACCESS_TOKEN)
          return { ...state, auth: { ...state.auth, accessToken: '', user: null } }
        }),
        
      refreshToken: initRefreshToken,
      setRefreshToken: (refreshToken) =>
        set((state) => {
          setCookie(REFRESH_TOKEN, JSON.stringify(refreshToken))
          return { ...state, auth: { ...state.auth, refreshToken } }
        }),

      reset: () =>
        set((state) => {
          removeCookie(ACCESS_TOKEN)
          removeCookie(REFRESH_TOKEN)
          return {
            ...state,
            auth: { ...state.auth, user: null, accessToken: '', refreshToken: '' },
          }
        }),
    },
  }
})
