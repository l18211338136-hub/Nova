import { useEffect, useRef, useState } from 'react'
import * as signalR from '@microsoft/signalr'
import { useAuthStore } from '@/stores/auth-store'
import { toast } from 'sonner'
import { useQueryClient } from '@tanstack/react-query'

export function useSignalR() {
  const { accessToken } = useAuthStore((state) => state.auth)
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null)
  const queryClient = useQueryClient()
  useEffect(() => {
    if (!accessToken) return

    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl((import.meta.env.VITE_API_URL || '') + '/api/hubs/notifications', {
        accessTokenFactory: () => accessToken,
      })
      .withAutomaticReconnect()
      .build()

    let isMounted = true;

    async function startConnection() {
      try {
        await newConnection.start()
        if (!isMounted) {
            await newConnection.stop()
            return
        }
        console.log('SignalR Connected!')
        setConnection(newConnection)
        
        newConnection.on('ReceiveNotification', (notification: any) => {
          toast(notification.title || '新通知', {
            description: notification.content || '',
          })
          // 刷新通知列表
          queryClient.invalidateQueries({ queryKey: ['getMyNotifications'] })
        })
      } catch (e) {
        console.error('SignalR Connection Error: ', e)
      }
    }

    startConnection()

    return () => {
      isMounted = false;
      if (newConnection.state === signalR.HubConnectionState.Connected) {
        newConnection.stop()
      }
    }
  }, [accessToken, queryClient])

  return connection
}
