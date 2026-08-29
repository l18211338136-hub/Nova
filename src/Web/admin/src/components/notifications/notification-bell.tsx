import { useState } from 'react'
import { Bell, Check, Loader2 } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { formatDistanceToNow } from 'date-fns'
import { zhCN, enUS } from 'date-fns/locale'

import { Button } from '@/components/ui/button'
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Badge } from '@/components/ui/badge'
import { Separator } from '@/components/ui/separator'

import {
  myNotifications,
  useMyNotifications,
  useMarkNotificationAsRead,
  useMarkAllNotificationsAsRead,
} from '@/api/endpoints/notifications'
import { useQueryClient, useInfiniteQuery } from '@tanstack/react-query'

export function NotificationBell() {
  const { t, i18n } = useTranslation()
  const [open, setOpen] = useState(false)
  const [selectedNotification, setSelectedNotification] = useState<any | null>(null)
  const queryClient = useQueryClient()

  // 1. Fetch notifications via Infinite Query
  const {
    data: res,
    isLoading,
    isFetchingNextPage,
    hasNextPage,
    fetchNextPage
  } = useInfiniteQuery({
    queryKey: ['getMyNotifications'], // Keeping the same key so useSignalR invalidation works
    queryFn: ({ pageParam = 0, signal }) => {
      return myNotifications({
        params: {
          $skip: pageParam,
          $top: 20,
          $orderby: 'CreatedAt desc'
        }
      }, signal)
    },
    getNextPageParam: (lastPage, allPages) => {
      const loadedCount = allPages.reduce((acc, page) => acc + (page.data?.items?.length || 0), 0)
      if (lastPage.data?.total && loadedCount < lastPage.data.total) {
        return loadedCount
      }
      return undefined
    },
    initialPageParam: 0,
    refetchInterval: 60000,
  })

  const notifications = res?.pages.flatMap(page => page.data?.items || []) || []
  
  // Fetch exact unread count via OData
  const { data: unreadCountRes } = useMyNotifications({
    query: {
      queryKey: ['getMyNotifications', 'unreadCount'],
      refetchInterval: 60000,
    },
    request: {
      params: {
        $filter: 'IsRead eq false',
        $top: 1, // Only need the total count
      }
    }
  })
  
  const unreadCount = unreadCountRes?.data?.total || 0
  // 2. Mutations
  const { mutate: markAsRead, isPending: isMarking } = useMarkNotificationAsRead({
    mutation: {
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: ['getMyNotifications'] })
      },
    },
  })

  const { mutate: markAllAsRead, isPending: isMarkingAll } = useMarkAllNotificationsAsRead({
    mutation: {
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: ['getMyNotifications'] })
      },
    },
  })

  const handleMarkAsRead = (notification: any) => {
    // Open details
    setSelectedNotification(notification)
    // Mark as read if not already
    if (!notification.isRead) {
      markAsRead({ id: notification.id })
    }
  }

  const handleMarkAllAsRead = () => {
    markAllAsRead()
  }

  const dateLocale = i18n.language.startsWith('zh') ? zhCN : enUS

  return (
    <>
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button variant='ghost' size='icon' className='relative'>
          <Bell className='h-5 w-5' />
          {unreadCount > 0 && (
            <Badge
              variant='destructive'
              className='absolute -top-1 -right-1 h-5 w-5 flex items-center justify-center rounded-full p-0 text-[10px]'
            >
              {unreadCount > 99 ? '99+' : unreadCount}
            </Badge>
          )}
        </Button>
      </PopoverTrigger>
      <PopoverContent className='w-80 p-0' align='end'>
        <div className='flex items-center justify-between px-4 py-3'>
          <h4 className='text-sm font-semibold'>{t('Notifications')}</h4>
          {unreadCount > 0 && (
            <Button
              variant='ghost'
              size='sm'
              className='h-auto px-2 py-1 text-xs text-muted-foreground'
              onClick={handleMarkAllAsRead}
              disabled={isMarkingAll}
            >
              {isMarkingAll ? <Loader2 className='mr-1 h-3 w-3 animate-spin' /> : <Check className='mr-1 h-3 w-3' />}
              {t('Mark all as read')}
            </Button>
          )}
        </div>
        <Separator />
        <ScrollArea 
          className='h-[350px]'
          onScrollCapture={(e) => {
            const target = e.target as HTMLDivElement;
            if (target.scrollHeight - target.scrollTop <= target.clientHeight + 50) {
              if (hasNextPage && !isFetchingNextPage) {
                fetchNextPage();
              }
            }
          }}
        >
          {isLoading ? (
            <div className='flex h-full items-center justify-center p-4 text-muted-foreground'>
              <Loader2 className='h-6 w-6 animate-spin' />
            </div>
          ) : notifications.length === 0 ? (
            <div className='flex h-full flex-col items-center justify-center p-8 text-center'>
              <Bell className='mb-2 h-8 w-8 text-muted-foreground/50' />
              <p className='text-sm text-muted-foreground'>
                {t('No notifications yet')}
              </p>
            </div>
          ) : (
            <div className='flex flex-col'>
              {notifications.map((notification: any) => (
                <div
                  key={notification.id}
                  onClick={() => handleMarkAsRead(notification)}
                  className={`flex flex-col gap-1 border-b p-4 last:border-0 hover:bg-muted/50 cursor-pointer transition-colors ${
                    !notification.isRead ? 'bg-primary/5' : ''
                  }`}
                >
                  <div className='flex items-start justify-between gap-2'>
                    <span className={`text-sm ${!notification.isRead ? 'font-semibold' : 'font-medium'}`}>
                      {notification.title}
                    </span>
                    {!notification.isRead && (
                      <span className='mt-1.5 h-2 w-2 shrink-0 rounded-full bg-primary'></span>
                    )}
                  </div>
                  <p className='text-xs text-muted-foreground line-clamp-2'>
                    {notification.content}
                  </p>
                  <span className='text-[10px] text-muted-foreground mt-1'>
                    {formatDistanceToNow(new Date(notification.createdAt), {
                      addSuffix: true,
                      locale: dateLocale,
                    })}
                  </span>
                </div>
              ))}
              {isFetchingNextPage && (
                <div className='flex items-center justify-center py-4'>
                  <Loader2 className='h-5 w-5 animate-spin text-muted-foreground' />
                </div>
              )}
            </div>
          )}
        </ScrollArea>
      </PopoverContent>
    </Popover>

    <Dialog open={!!selectedNotification} onOpenChange={(isOpen) => !isOpen && setSelectedNotification(null)}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>{selectedNotification?.title}</DialogTitle>
          <DialogDescription>
            {selectedNotification?.createdAt && formatDistanceToNow(new Date(selectedNotification.createdAt), {
              addSuffix: true,
              locale: dateLocale,
            })}
          </DialogDescription>
        </DialogHeader>
        <div className="py-4">
          <p className="text-sm whitespace-pre-wrap">{selectedNotification?.content}</p>
          
          {selectedNotification?.payloadJson && (
            <div className="mt-4 p-3 bg-muted rounded-md overflow-x-auto">
              <pre className="text-xs text-muted-foreground">
                {selectedNotification.payloadJson}
              </pre>
            </div>
          )}
        </div>
      </DialogContent>
    </Dialog>
    </>
  )
}
