import { useEffect, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { fonts } from '@/config/fonts'
import { useFont } from '@/context/font-provider'
import { useTheme } from '@/context/theme-provider'
import { usePreferences } from '@/hooks/use-preferences'

const THEMES = ['light', 'dark', 'system'] as const
type Theme = (typeof THEMES)[number]
type Font = (typeof fonts)[number]

/**
 * 把服务端保存的外观偏好同步到本地 provider，实现跨设备一致。
 *
 * 只在每次会话首次拿到偏好时同步一次：之后用户在本地切主题（顶栏的主题开关等）
 * 不应该被这次同步反复覆盖回去。
 */
export function PreferencesSync() {
  const { preferences, isLoading } = usePreferences()
  const { theme, setTheme } = useTheme()
  const { font, setFont } = useFont()
  const { i18n } = useTranslation()
  const syncedRef = useRef(false)

  useEffect(() => {
    if (isLoading || syncedRef.current) return
    syncedRef.current = true

    const nextTheme = preferences.theme as Theme
    if (THEMES.includes(nextTheme) && nextTheme !== theme) {
      setTheme(nextTheme)
    }

    const nextFont = preferences.font as Font
    if ((fonts as readonly string[]).includes(nextFont) && nextFont !== font) {
      setFont(nextFont)
    }

    const nextLanguage = preferences.language
    if (nextLanguage && nextLanguage !== i18n.language) {
      i18n.changeLanguage(nextLanguage)
      localStorage.setItem('i18nextLng', nextLanguage)
    }
    // 仅依赖加载状态与服务端值；本地 theme/font 只作为比较基准，不触发重跑
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isLoading, preferences.theme, preferences.font, preferences.language])

  return null
}
