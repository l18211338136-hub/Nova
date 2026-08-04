import { useCallback } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import {
  useGetPreferences,
  useUpdatePreferences,
  getGetPreferencesQueryKey,
} from '@/api/endpoints/profile'
import type { UpdatePreferences, UserPreferenceDto } from '@/api/model'
import { useAuthStore } from '@/stores/auth-store'

/**
 * 后端未落库时的兜底默认值，与 UserPreference.CreateDefault 保持一致。
 * 后端在首次访问时也会返回同样的默认值，这里只是为了让加载中的表单有稳定的初值。
 */
export const DEFAULT_PREFERENCES = {
  theme: 'system',
  font: 'inter',
  language: 'zh-CN',
  timeZone: null,
  notifyType: 'all',
  communicationEmails: false,
  marketingEmails: false,
  socialEmails: true,
  securityEmails: true,
  mobileNotifications: false,
  hiddenSidebarItems: [],
} satisfies NonNullable<UserPreferenceDto>

export type Preferences = NonNullable<UserPreferenceDto>

/**
 * 读取当前登录用户的偏好设置。
 *
 * 未登录时不发请求（避免 401 噪音），直接返回默认值。
 */
export function usePreferences() {
  const accessToken = useAuthStore((s) => s.auth.accessToken)
  const enabled = Boolean(accessToken)

  const query = useGetPreferences({
    query: {
      enabled,
      staleTime: 5 * 60 * 1000,
      // 偏好不是高频变化的数据，切页面时不必反复拉取
      refetchOnWindowFocus: false,
    },
  })

  const preferences: Preferences = {
    ...DEFAULT_PREFERENCES,
    ...(query.data?.data ?? {}),
  }

  return {
    preferences,
    // 未登录时不会发请求，此时不应把 UI 卡在 loading 态
    isLoading: enabled && query.isLoading,
    isError: query.isError,
  }
}

/**
 * 提交偏好设置的局部更新。
 *
 * 后端约定「字段为 null 即不修改」，因此各设置页只需提交自己关心的字段，
 * 不会覆盖其他页面的设置。
 */
export function useSavePreferences(options?: { successMessage?: string }) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()

  const mutation = useUpdatePreferences({
    mutation: {
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: getGetPreferencesQueryKey() })
        toast.success(options?.successMessage ?? t('Preferences updated.'))
      },
      onError: (error: unknown) => {
        toast.error(resolveErrorMessage(error, t('Failed to update preferences.')))
      },
    },
  })

  const save = useCallback(
    (data: UpdatePreferences) => mutation.mutate({ data }),
    [mutation]
  )

  return { save, isPending: mutation.isPending }
}

/** 从 axios 错误对象中提取后端返回的 message，取不到时回落到默认文案。 */
export function resolveErrorMessage(error: unknown, fallback: string): string {
  const err = error as
    | { response?: { data?: { message?: string } }; message?: string }
    | undefined
  return err?.response?.data?.message || err?.message || fallback
}
