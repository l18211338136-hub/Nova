import { useEffect } from 'react'
import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import {
  useGetProfile,
  useUpdateProfile,
  getGetProfileQueryKey,
} from '@/api/endpoints/profile'
import { resolveErrorMessage } from '@/hooks/use-preferences'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import {
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import { Input } from '@/components/ui/input'
import { Skeleton } from '@/components/ui/skeleton'
import { Textarea } from '@/components/ui/textarea'
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar'

const getProfileFormSchema = (t: (arg: string) => string) =>
  z.object({
    nickName: z
      .string()
      .max(50, t('Nickname must not be longer than 50 characters.'))
      .optional(),
    bio: z
      .string()
      .max(160, t('Bio must not be longer than 160 characters.'))
      .optional(),
    avatarUrl: z
      .union([z.url(t('Please enter a valid URL.')), z.literal('')])
      .optional(),
    phoneNumber: z
      .union([
        z.string().regex(/^1[3-9]\d{9}$/, t('Please enter a valid phone number.')),
        z.literal(''),
      ])
      .optional(),
  })

type ProfileFormValues = z.infer<ReturnType<typeof getProfileFormSchema>>

const EMPTY_VALUES: ProfileFormValues = {
  nickName: '',
  bio: '',
  avatarUrl: '',
  phoneNumber: '',
}

export function ProfileForm() {
  const { t } = useTranslation()
  const queryClient = useQueryClient()

  const { data, isLoading } = useGetProfile()
  const profile = data?.data

  const form = useForm<ProfileFormValues>({
    resolver: zodResolver(getProfileFormSchema(t)),
    defaultValues: EMPTY_VALUES,
    mode: 'onChange',
  })

  // 资料是异步拉取的，到货后再回填表单（reset 会同时刷新 dirty 基线）
  useEffect(() => {
    if (!profile) return
    form.reset({
      nickName: profile.nickName ?? '',
      bio: profile.bio ?? '',
      avatarUrl: profile.avatarUrl ?? '',
      phoneNumber: profile.phoneNumber ?? '',
    })
  }, [profile, form])

  const updateMutation = useUpdateProfile({
    mutation: {
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: getGetProfileQueryKey() })
        toast.success(t('Profile updated.'))
      },
      onError: (error: unknown) => {
        toast.error(resolveErrorMessage(error, t('Failed to update profile.')))
      },
    },
  })

  function onSubmit(values: ProfileFormValues) {
    updateMutation.mutate({
      data: {
        // 空字符串代表「清空该项」，统一转成 null 交给后端
        nickName: values.nickName?.trim() || null,
        bio: values.bio?.trim() || null,
        avatarUrl: values.avatarUrl?.trim() || null,
        phoneNumber: values.phoneNumber?.trim() || null,
      },
    })
  }

  if (isLoading) {
    return (
      <div className='space-y-6'>
        <Skeleton className='h-16 w-full' />
        <Skeleton className='h-10 w-full' />
        <Skeleton className='h-10 w-full' />
        <Skeleton className='h-24 w-full' />
      </div>
    )
  }

  const avatarPreview = form.watch('avatarUrl')
  const displayName = profile?.nickName || profile?.userName || ''

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className='space-y-8'>
        {/* 只读的账号概览：这些字段由管理员或注册流程决定，不在此页修改 */}
        <div className='flex items-center gap-4 rounded-lg border p-4'>
          <Avatar className='size-14'>
            <AvatarImage src={avatarPreview || undefined} alt={displayName} />
            <AvatarFallback>
              {displayName.slice(0, 2).toUpperCase() || 'NA'}
            </AvatarFallback>
          </Avatar>
          <div className='space-y-1'>
            <div className='flex flex-wrap items-center gap-2'>
              <span className='font-medium'>{profile?.userName}</span>
              {profile?.roles?.map((role) => (
                <Badge key={role.name} variant='secondary'>
                  {role.displayName || role.name}
                </Badge>
              ))}
            </div>
            <div className='text-sm text-muted-foreground'>
              {profile?.email || t('No email bound')}
              {profile?.email && !profile.emailConfirmed && (
                <span className='ms-2 text-amber-600'>{t('Unverified')}</span>
              )}
            </div>
          </div>
        </div>

        <FormField
          control={form.control}
          name='nickName'
          render={({ field }) => (
            <FormItem>
              <FormLabel>{t('Nickname')}</FormLabel>
              <FormControl>
                <Input
                  placeholder={profile?.userName ?? ''}
                  {...field}
                  value={field.value ?? ''}
                />
              </FormControl>
              <FormDescription>
                {t('This is your public display name. Leave it empty to use your username.')}
              </FormDescription>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name='phoneNumber'
          render={({ field }) => (
            <FormItem>
              <FormLabel>{t('Phone number')}</FormLabel>
              <FormControl>
                <Input
                  placeholder='13800000000'
                  {...field}
                  value={field.value ?? ''}
                />
              </FormControl>
              <FormDescription>
                {t('Your phone number can be used to sign in and to locate your tenant.')}
              </FormDescription>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name='avatarUrl'
          render={({ field }) => (
            <FormItem>
              <FormLabel>{t('Avatar URL')}</FormLabel>
              <FormControl>
                <Input
                  placeholder='https://example.com/avatar.png'
                  {...field}
                  value={field.value ?? ''}
                />
              </FormControl>
              <FormDescription>
                {t('Paste an image link to use as your avatar.')}
              </FormDescription>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name='bio'
          render={({ field }) => (
            <FormItem>
              <FormLabel>{t('Bio')}</FormLabel>
              <FormControl>
                <Textarea
                  placeholder={t('Tell us a little bit about yourself')}
                  className='resize-none'
                  {...field}
                  value={field.value ?? ''}
                />
              </FormControl>
              <FormDescription>
                {t('Up to 160 characters.')}
              </FormDescription>
              <FormMessage />
            </FormItem>
          )}
        />

        <Button type='submit' disabled={updateMutation.isPending}>
          {updateMutation.isPending ? t('Updating...') : t('Update profile')}
        </Button>
      </form>
    </Form>
  )
}
