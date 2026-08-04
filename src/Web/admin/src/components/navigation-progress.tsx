import { useEffect, useRef } from 'react'
import { useRouterState } from '@tanstack/react-router'
import LoadingBar, { type LoadingBarRef } from 'react-top-loading-bar'

export function NavigationProgress() {
  const ref = useRef<LoadingBarRef>(null)
  const state = useRouterState()
  // 记录上一次"导航完成(idle)"时的路径，用于区分真正的页面跳转与仅 search 参数变化
  const lastSettledPath = useRef(state.location.pathname)
  // 标记进度条是否被真正启动过。react-top-loading-bar 的 complete() 在未 start 时
  // 仍会闪出一条满进度条再淡出，因此只有真正 start 过才允许 complete，避免筛选/分页时闪烁。
  const isActive = useRef(false)

  useEffect(() => {
    const currentPath = state.location.pathname

    if (state.status === 'pending') {
      // 仅在目标路径与上次 settle 的路径不同（真正切换页面）时才显示顶部进度条。
      // 仅 search 参数变化（筛选 / 分页 / 排序）不会改变 pathname，不展示，避免频繁闪烁。
      if (currentPath !== lastSettledPath.current) {
        ref.current?.continuousStart()
        isActive.current = true
      }
    } else {
      // 导航完成：记录已 settle 的路径，仅当本次确实启动过进度条时才结束它
      lastSettledPath.current = currentPath
      if (isActive.current) {
        ref.current?.complete()
        isActive.current = false
      }
    }
  }, [state.status, state.location.pathname])

  return (
    <LoadingBar
      color='var(--muted-foreground)'
      ref={ref}
      shadow={true}
      height={2}
    />
  )
}
