import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useNavigate, useSearch } from '@tanstack/react-router'
import { Loader2, KeyRound } from 'lucide-react'
import { toast } from 'sonner'
import { cn } from '@/lib/utils'
import { useResetPassword, useSendForgotPasswordCode } from '@/api/endpoints/identity/identity'
import { Button } from '@/components/ui/button'
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import { Input } from '@/components/ui/input'
import { PasswordInput } from '@/components/password-input'
import {
  InputOTP,
  InputOTPGroup,
  InputOTPSlot,
  InputOTPSeparator,
} from '@/components/ui/input-otp'

const getFormSchema = (t: (arg: string) => string) => z.object({
  email: z.email({
    error: (iss) => (iss.input === '' ? t('Please enter your email.') : undefined),
  }),
  code: z.string().min(6, t('Please enter the 6-digit code.')),
  newPassword: z
    .string()
    .min(1, t('Please enter your password.'))
    .min(7, t('Password must be at least 7 characters long.')),
})

type ResetPasswordFormProps = React.HTMLAttributes<HTMLFormElement>

export function ResetPasswordForm({ className, ...props }: ResetPasswordFormProps) {
  const { t } = useTranslation()
  const search = useSearch({ from: '/(auth)/reset-password' })
  const formSchema = getFormSchema(t)
  const navigate = useNavigate()
  const [isLoading, setIsLoading] = useState(false)
  const [isSendingCode, setIsSendingCode] = useState(false)
  const [countdown, setCountdown] = useState(60) // Assume they just came from sending

  const { mutateAsync: resetPassword } = useResetPassword()
  const { mutateAsync: sendCode } = useSendForgotPasswordCode()

  const form = useForm<z.infer<typeof formSchema>>({
    resolver: zodResolver(formSchema),
    defaultValues: { email: search.email || '', code: '', newPassword: '' },
  })

  useEffect(() => {
    let timer: NodeJS.Timeout
    if (countdown > 0) {
      timer = setTimeout(() => setCountdown(countdown - 1), 1000)
    }
    return () => clearTimeout(timer)
  }, [countdown])

  async function handleSendCode() {
    const email = form.getValues('email')
    const result = z.string().email().safeParse(email)
    if (!result.success) {
      form.setError('email', { type: 'manual', message: t('Please enter a valid email.') })
      return
    }

    setIsSendingCode(true)
    try {
      const res = await sendCode({ data: { email } })
      if (res.code === 200) {
        toast.success(t('Verification code sent successfully.'))
        setCountdown(60)
      } else {
        toast.error(res.message || t('Failed to send verification code.'))
      }
    } catch (error: any) {
    } finally {
      setIsSendingCode(false)
    }
  }

  async function onSubmit(data: z.infer<typeof formSchema>) {
    setIsLoading(true)
    try {
      const response = await resetPassword({
        data: { email: data.email, code: data.code, newPassword: data.newPassword },
      })
      if (response.code === 200) {
        toast.success(t('Password reset successful! Please sign in.'))
        navigate({ to: '/sign-in' })
      } else {
        toast.error(response.message || t('Failed to reset password'))
      }
    } catch (error: any) {
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <Form {...form}>
      <form
        onSubmit={form.handleSubmit(onSubmit)}
        className={cn('grid gap-4', className)}
        {...props}
      >
        <FormField
          control={form.control}
          name='email'
          render={({ field }) => (
            <FormItem>
              <FormLabel>{t('Email')}</FormLabel>
              <FormControl>
                <Input placeholder='name@example.com' {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        
        <FormField
          control={form.control}
          name='code'
          render={({ field }) => (
            <FormItem>
              <FormLabel>{t('Verification Code')}</FormLabel>
              <div className='flex gap-2 flex-col sm:flex-row sm:items-center justify-between'>
                <FormControl>
                  <InputOTP maxLength={6} {...field} containerClassName='justify-between'>
                    <InputOTPGroup>
                      <InputOTPSlot index={0} />
                      <InputOTPSlot index={1} />
                      <InputOTPSlot index={2} />
                      <InputOTPSlot index={3} />
                      <InputOTPSlot index={4} />
                      <InputOTPSlot index={5} />
                    </InputOTPGroup>
                  </InputOTP>
                </FormControl>
                <Button 
                  type='button' 
                  variant='outline' 
                  className='shrink-0'
                  disabled={countdown > 0 || isSendingCode} 
                  onClick={handleSendCode}
                >
                  {countdown > 0 ? `${countdown}s` : (isSendingCode ? <Loader2 className="h-4 w-4 animate-spin" /> : t('Resend Code'))}
                </Button>
              </div>
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
                <PasswordInput placeholder='********' {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <Button className='mt-2' disabled={isLoading}>
          {isLoading ? <Loader2 className='animate-spin' /> : <KeyRound />}
          {t('Reset Password')}
        </Button>
      </form>
    </Form>
  )
}
