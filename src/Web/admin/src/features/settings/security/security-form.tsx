import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { toast } from 'sonner'
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
import { useChangePassword } from '@/api/endpoints/auth'

const getSecurityFormSchema = (t: (arg: string) => string) =>
  z
    .object({
      oldPassword: z.string().min(1, t('Current password is required.')),
      newPassword: z
        .string()
        .min(7, t('Password must be at least 7 characters long.')),
      confirmPassword: z.string().min(1, t('Confirm Password is required.')),
    })
    .refine((data) => data.newPassword === data.confirmPassword, {
      message: t("Passwords don't match."),
      path: ['confirmPassword'],
    })

type SecurityFormValues = z.infer<ReturnType<typeof getSecurityFormSchema>>

export function SecurityForm() {
  const { t } = useTranslation()
  const form = useForm<SecurityFormValues>({
    resolver: zodResolver(getSecurityFormSchema(t)),
    defaultValues: { oldPassword: '', newPassword: '', confirmPassword: '' },
  })

  const changePasswordMutation = useChangePassword({
    mutation: {
      onSuccess: () => {
        toast.success(t('Password changed successfully.'))
        form.reset()
      },
      onError: (error: any) => {
        const msg =
          error?.response?.data?.message ||
          error?.message ||
          t('Failed to change password.')
        toast.error(msg)
      },
    },
  })

  function onSubmit(values: SecurityFormValues) {
    changePasswordMutation.mutate({
      data: {
        oldPassword: values.oldPassword,
        newPassword: values.newPassword,
      },
    })
  }

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className='space-y-8'>
        <FormField
          control={form.control}
          name='oldPassword'
          render={({ field }) => (
            <FormItem>
              <FormLabel>{t('Current Password')}</FormLabel>
              <FormControl>
                <Input type='password' autoComplete='current-password' {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        <FormField
          control={form.control}
          name='newPassword'
          render={({ field }) => (
            <FormItem>
              <FormLabel>{t('New Password')}</FormLabel>
              <FormControl>
                <Input type='password' autoComplete='new-password' {...field} />
              </FormControl>
              <FormDescription>
                {t('Password must be at least 7 characters long.')}
              </FormDescription>
              <FormMessage />
            </FormItem>
          )}
        />
        <FormField
          control={form.control}
          name='confirmPassword'
          render={({ field }) => (
            <FormItem>
              <FormLabel>{t('Confirm Password')}</FormLabel>
              <FormControl>
                <Input
                  type='password'
                  autoComplete='new-password'
                  {...field}
                />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        <Button type='submit' disabled={changePasswordMutation.isPending}>
          {changePasswordMutation.isPending
            ? t('Updating...')
            : t('Update password')}
        </Button>
      </form>
    </Form>
  )
}
