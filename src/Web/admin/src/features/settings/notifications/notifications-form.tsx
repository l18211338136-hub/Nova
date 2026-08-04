import { useEffect } from 'react'
import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { usePreferences, useSavePreferences } from '@/hooks/use-preferences'
import { Button } from '@/components/ui/button'
import { Checkbox } from '@/components/ui/checkbox'
import {
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group'
import { Skeleton } from '@/components/ui/skeleton'
import { Switch } from '@/components/ui/switch'

const notificationsFormSchema = z.object({
  type: z.enum(['all', 'mentions', 'none']),
  mobile: z.boolean(),
  communicationEmails: z.boolean(),
  socialEmails: z.boolean(),
  marketingEmails: z.boolean(),
})

type NotificationsFormValues = z.infer<typeof notificationsFormSchema>

export function NotificationsForm() {
  const { t } = useTranslation()
  const { preferences, isLoading } = usePreferences()
  const { save, isPending } = useSavePreferences({
    successMessage: t('Notification settings updated.'),
  })

  const form = useForm<NotificationsFormValues>({
    resolver: zodResolver(notificationsFormSchema),
    defaultValues: {
      type: 'all',
      mobile: false,
      communicationEmails: false,
      socialEmails: true,
      marketingEmails: false,
    },
  })

  useEffect(() => {
    if (isLoading) return
    form.reset({
      type: (preferences.notifyType as NotificationsFormValues['type']) ?? 'all',
      mobile: preferences.mobileNotifications ?? false,
      communicationEmails: preferences.communicationEmails ?? false,
      socialEmails: preferences.socialEmails ?? true,
      marketingEmails: preferences.marketingEmails ?? false,
    })
  }, [
    isLoading,
    preferences.notifyType,
    preferences.mobileNotifications,
    preferences.communicationEmails,
    preferences.socialEmails,
    preferences.marketingEmails,
    form,
  ])

  function onSubmit(values: NotificationsFormValues) {
    // 安全提醒邮件强制开启，后端也不接受修改，因此不提交该字段
    save({
      notifyType: values.type,
      mobileNotifications: values.mobile,
      communicationEmails: values.communicationEmails,
      socialEmails: values.socialEmails,
      marketingEmails: values.marketingEmails,
    })
  }

  if (isLoading) {
    return (
      <div className='space-y-6'>
        <Skeleton className='h-24 w-full' />
        <Skeleton className='h-20 w-full' />
        <Skeleton className='h-20 w-full' />
      </div>
    )
  }

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className='space-y-8'>
        <FormField
          control={form.control}
          name='type'
          render={({ field }) => (
            <FormItem className='relative space-y-3'>
              <FormLabel>{t('Notify me about...')}</FormLabel>
              <FormControl>
                <RadioGroup
                  onValueChange={field.onChange}
                  value={field.value}
                  className='flex flex-col gap-2'
                >
                  <FormItem className='flex items-center'>
                    <FormControl>
                      <RadioGroupItem value='all' />
                    </FormControl>
                    <FormLabel className='font-normal'>
                      {t('All new messages')}
                    </FormLabel>
                  </FormItem>
                  <FormItem className='flex items-center'>
                    <FormControl>
                      <RadioGroupItem value='mentions' />
                    </FormControl>
                    <FormLabel className='font-normal'>
                      {t('Direct messages and mentions')}
                    </FormLabel>
                  </FormItem>
                  <FormItem className='flex items-center'>
                    <FormControl>
                      <RadioGroupItem value='none' />
                    </FormControl>
                    <FormLabel className='font-normal'>{t('Nothing')}</FormLabel>
                  </FormItem>
                </RadioGroup>
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        <div className='relative'>
          <h3 className='mb-4 text-lg font-medium'>
            {t('Email Notifications')}
          </h3>
          <div className='space-y-4'>
            <FormField
              control={form.control}
              name='communicationEmails'
              render={({ field }) => (
                <FormItem className='flex flex-row items-center justify-between rounded-lg border p-4'>
                  <div className='space-y-0.5'>
                    <FormLabel className='text-base'>
                      {t('Communication emails')}
                    </FormLabel>
                    <FormDescription>
                      {t('Receive emails about your account activity.')}
                    </FormDescription>
                  </div>
                  <FormControl>
                    <Switch
                      checked={field.value}
                      onCheckedChange={field.onChange}
                    />
                  </FormControl>
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name='marketingEmails'
              render={({ field }) => (
                <FormItem className='flex flex-row items-center justify-between rounded-lg border p-4'>
                  <div className='space-y-0.5'>
                    <FormLabel className='text-base'>
                      {t('Marketing emails')}
                    </FormLabel>
                    <FormDescription>
                      {t('Receive emails about new products, features, and more.')}
                    </FormDescription>
                  </div>
                  <FormControl>
                    <Switch
                      checked={field.value}
                      onCheckedChange={field.onChange}
                    />
                  </FormControl>
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name='socialEmails'
              render={({ field }) => (
                <FormItem className='flex flex-row items-center justify-between rounded-lg border p-4'>
                  <div className='space-y-0.5'>
                    <FormLabel className='text-base'>
                      {t('Social emails')}
                    </FormLabel>
                    <FormDescription>
                      {t('Receive emails for friend requests, follows, and more.')}
                    </FormDescription>
                  </div>
                  <FormControl>
                    <Switch
                      checked={field.value}
                      onCheckedChange={field.onChange}
                    />
                  </FormControl>
                </FormItem>
              )}
            />
            <FormItem className='flex flex-row items-center justify-between rounded-lg border p-4'>
              <div className='space-y-0.5'>
                <FormLabel className='text-base'>
                  {t('Security emails')}
                </FormLabel>
                <FormDescription>
                  {t('Security emails are always on and cannot be turned off.')}
                </FormDescription>
              </div>
              <Switch checked disabled aria-readonly />
            </FormItem>
          </div>
        </div>
        <FormField
          control={form.control}
          name='mobile'
          render={({ field }) => (
            <FormItem className='relative flex flex-row items-start'>
              <FormControl>
                <Checkbox
                  checked={field.value}
                  onCheckedChange={field.onChange}
                />
              </FormControl>
              <div className='space-y-1 leading-none'>
                <FormLabel>
                  {t('Use different settings for my mobile devices')}
                </FormLabel>
                <FormDescription>
                  {t('Mobile push will follow this switch instead of the settings above.')}
                </FormDescription>
              </div>
            </FormItem>
          )}
        />
        <Button type='submit' disabled={isPending}>
          {isPending ? t('Updating...') : t('Update notifications')}
        </Button>
      </form>
    </Form>
  )
}
