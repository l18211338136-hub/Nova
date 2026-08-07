import { useEffect, useState } from 'react'
import { useForm, SubmitErrorHandler } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { zodResolver } from '@hookform/resolvers/zod'
import { useQueryClient } from '@tanstack/react-query'
import { Camera, Loader2 } from 'lucide-react'
import { toast } from 'sonner'
import { z } from 'zod'
import {
  useGetProfile,
  useUpdateProfile,
  getGetProfileQueryKey,
} from '@/api/endpoints/profile'
import { useUpload } from '@/api/endpoints/storage'
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
import { Textarea } from '@/components/ui/textarea'
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar'
import { AvatarCropDialog } from './avatar-crop-dialog'

import { getFullImageUrl } from '@/lib/utils'

const getProfileFormSchema = (t: (key: string) => string) =>
  z.object({
    nickName: z
      .string()
      .max(64, t('Nickname must not exceed 64 characters.'))
      .optional(),
    bio: z
      .string()
      .max(160, t('Bio must not be longer than 160 characters.'))
      .optional(),
    avatarUrl: z.string().optional(),
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
  const [isUploading, setIsUploading] = useState(false)
  const [avatarFileId, setAvatarFileId] = useState<string | undefined>()
  const [previewAvatarUrl, setPreviewAvatarUrl] = useState<string | null>(null)

  // 裁剪弹窗相关 state
  const [cropDialogOpen, setCropDialogOpen] = useState(false)
  const [rawImageSrc, setRawImageSrc] = useState<string | null>(null)
  const [rawFileName, setRawFileName] = useState('avatar.png')

  const { data, isLoading } = useGetProfile()
  const profile = data?.data

  const form = useForm<ProfileFormValues>({
    resolver: zodResolver(getProfileFormSchema(t)),
    defaultValues: EMPTY_VALUES,
    mode: 'onChange',
  })

  // 资料是异步拉取的，到货后再回填表单
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
        queryClient.invalidateQueries({
          queryKey: getGetProfileQueryKey(),
          refetchType: 'all',
        })
        toast.success(t('Profile updated.'))
      },
      onError: (error: unknown) => {
        toast.error(resolveErrorMessage(error, t('Failed to update profile.')))
      },
    },
  })

  const uploadMutation = useUpload()

  // 1. 用户选择本地图片 -> 唤起裁剪 Modal 弹窗
  const handleSelectFile = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return

    setRawFileName(file.name)
    const objectUrl = URL.createObjectURL(file)
    setRawImageSrc(objectUrl)
    setCropDialogOpen(true)

    // 重置 input value 方便重复选同一文件
    e.target.value = ''
  }

  // 2. 在裁剪 Modal 确认后，执行裁切文件上传
  const handleConfirmCrop = async (croppedFile: File, previewUrl: string) => {
    try {
      setIsUploading(true)
      setPreviewAvatarUrl(previewUrl)

      const uploadRes = await uploadMutation.mutateAsync({
        data: { file: croppedFile },
      })

      const fileObj = uploadRes.data
      if (!fileObj) return

      const finalUrl = fileObj.accessUrl || previewUrl
      form.setValue('avatarUrl', finalUrl, { shouldDirty: true })
      setAvatarFileId(fileObj.id)

      toast.success(t('Avatar uploaded successfully.'))
    } catch {
      toast.error(t('Failed to upload avatar.'))
    } finally {
      setIsUploading(false)
    }
  }

  function onSubmit(values: ProfileFormValues, e?: React.BaseSyntheticEvent) {
    e?.preventDefault()
    if (updateMutation.isPending || isUploading) return

    updateMutation.mutate({
      data: {
        nickName: values.nickName,
        bio: values.bio,
        avatarUrl: values.avatarUrl,
        avatarFileId,
        phoneNumber: values.phoneNumber,
      },
    })
  }

  const onInvalid: SubmitErrorHandler<ProfileFormValues> = (errors) => {
    const firstField = Object.keys(errors)[0]
    const message = errors[firstField as keyof ProfileFormValues]?.message
    if (message) {
      toast.error(message)
    }
  }

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit, onInvalid)} className='space-y-6'>
        {/* 头像预览与实时上传交互卡片 */}
        <div className='flex items-center gap-6 rounded-lg border p-4 bg-muted/20'>
          <div className='relative group'>
            <Avatar className='h-20 w-20 border-2 border-border shadow-sm'>
              <AvatarImage
                src={getFullImageUrl(previewAvatarUrl || form.watch('avatarUrl') || profile?.avatarUrl)}
                alt='Avatar'
              />
              <AvatarFallback className='bg-primary/10 text-primary font-bold text-xl'>
                {profile?.userName?.substring(0, 2).toUpperCase() || 'U'}
              </AvatarFallback>
            </Avatar>

            <label
              htmlFor='avatar-file-input'
              className='absolute inset-0 rounded-full bg-black/40 flex items-center justify-center text-white opacity-0 group-hover:opacity-100 transition-opacity cursor-pointer'
            >
              {isUploading ? (
                <Loader2 className='h-6 w-6 animate-spin' />
              ) : (
                <Camera className='h-6 w-6' />
              )}
            </label>

            <input
              id='avatar-file-input'
              type='file'
              accept='image/*'
              className='hidden'
              onChange={handleSelectFile}
              disabled={isUploading}
            />
          </div>

          <div className='space-y-1.5'>
            <div className='flex items-center gap-2 flex-wrap'>
              <h4 className='font-semibold text-base'>{profile?.userName}</h4>
              {profile?.roles && profile.roles.length > 0 && (
                <div className='flex flex-wrap gap-1'>
                  {profile.roles.map((role) => (
                    <Badge
                      key={role.name || role.displayName}
                      variant='outline'
                      className='text-[10px] font-normal'
                    >
                      {role.displayName || role.name}
                    </Badge>
                  ))}
                </div>
              )}
            </div>
            <p className='text-xs text-muted-foreground hover:text-primary transition-colors'>
              {t('Click the avatar above to select a new image.')}
            </p>
          </div>
        </div>

        <FormField
          control={form.control}
          name='nickName'
          render={({ field }) => (
            <FormItem>
              <FormLabel>{t('Nickname')}</FormLabel>
              <FormControl>
                <Input placeholder={t('Enter your nickname')} {...field} />
              </FormControl>
              <FormDescription>
                {t('This name will be displayed in the system.')}
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
              <FormLabel>{t('Phone Number')}</FormLabel>
              <FormControl>
                <Input placeholder={t('Enter your phone number')} {...field} />
              </FormControl>
              <FormDescription>
                {t('Used for system notification and login.')}
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
                  placeholder={t('Tell us a little about yourself')}
                  className='resize-none'
                  {...field}
                />
              </FormControl>
              <FormDescription>
                {t('Brief description for your profile.')}
              </FormDescription>
              <FormMessage />
            </FormItem>
          )}
        />

        <Button
          type='submit'
          disabled={isLoading || updateMutation.isPending || isUploading}
        >
          {(isLoading || updateMutation.isPending) && (
            <Loader2 className='mr-2 h-4 w-4 animate-spin' />
          )}
          {t('Save changes')}
        </Button>

        {/* 头像编辑裁切对话框 Modal */}
        <AvatarCropDialog
          open={cropDialogOpen}
          imageSrc={rawImageSrc}
          fileName={rawFileName}
          onClose={() => setCropDialogOpen(false)}
          onConfirm={handleConfirmCrop}
        />
      </form>
    </Form>
  )
}
