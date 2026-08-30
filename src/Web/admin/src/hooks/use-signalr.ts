import { useEffect, useState } from 'react'
import * as signalR from '@microsoft/signalr'
import { useAuthStore } from '@/stores/auth-store'
import { toast } from 'sonner'
import { useQueryClient } from '@tanstack/react-query'

let globalConnection: signalR.HubConnection | null = null;
let connectionPromise: Promise<void> | null = null;

export function useSignalR() {
  const { accessToken } = useAuthStore((state) => state.auth)
  const [connection, setConnection] = useState<signalR.HubConnection | null>(globalConnection)
  const queryClient = useQueryClient()

  useEffect(() => {
    if (!accessToken) {
      if (globalConnection) {
        globalConnection.stop()
        globalConnection = null;
        connectionPromise = null;
        setConnection(null)
      }
      return
    }

    if (globalConnection || connectionPromise) {
      if (globalConnection) setConnection(globalConnection)
      return
    }

    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl((import.meta.env.VITE_API_URL || '') + '/api/hubs/notifications', {
        accessTokenFactory: () => accessToken,
      })
      .withAutomaticReconnect()
      .build()

    async function startConnection() {
      try {
        await newConnection.start()
        console.log('SignalR Connected!')
        globalConnection = newConnection;
        setConnection(newConnection)
        
        // Request desktop notification permission
        if ('Notification' in window && Notification.permission === 'default') {
          Notification.requestPermission()
        }
        
        newConnection.on('ReceiveNotification', (notification: any) => {
          const title = notification.title || '新通知'
          const content = notification.content || ''
          
          toast(title, {
            description: content,
          })

          // Show desktop notification if page is not focused and permitted
          if ('Notification' in window && Notification.permission === 'granted' && !document.hasFocus()) {
            new Notification(title, {
              body: content,
              icon: window.location.origin + '/vite.svg', // Fallback icon
            })
          }

          // 刷新通知列表
          queryClient.invalidateQueries({ queryKey: ['getMyNotifications'] })
        })
      } catch (e) {
        console.error('SignalR Connection Error: ', e)
        connectionPromise = null; // 失败后允许重试
      }
    }

    connectionPromise = startConnection()

    return () => {
      // 避免在开发环境 StrictMode 下频繁断开重连，交由全局管理生命周期
    }
  }, [accessToken, queryClient])

  return connection
}
