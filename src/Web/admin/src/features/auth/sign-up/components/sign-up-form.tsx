import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Loader2, UserPlus } from 'lucide-react'
import { toast } from 'sonner'
import { IconFacebook, IconGithub } from '@/assets/brand-icons'
import { cn } from '@/lib/utils'
import { useRegister, useSendRegisterCode } from '@/api/endpoints/identity/identity'
import { useRouter } from '@tanstack/react-router'
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

const getFormSchema = (t: (arg: string) => string) => z
  .object({
    email: z.email({
      error: (iss) =>
        iss.input === '' ? t('Please enter your email.') : undefined,
    }),
    password: z
      .string()
      .min(1, t('Please enter your password.'))
      .min(7, t('Password must be at least 7 characters long.')),
    confirmPassword: z.string().min(1, t('Please confirm your password.')),
    emailCode: z.string().min(1, t('Please enter the verification code.')),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: t("Passwords don't match."),
    path: ['confirmPassword'],
  })

export function SignUpForm({
  className,
  ...props
}: React.HTMLAttributes<HTMLFormElement>) {
  const { t } = useTranslation()
  const formSchema = getFormSchema(t)
  const [isLoading, setIsLoading] = useState(false)
  const { mutateAsync: register } = useRegister()
  const { mutateAsync: sendCode } = useSendRegisterCode()
  const router = useRouter()
  const [countdown, setCountdown] = useState(0)

  useEffect(() => {
    let timer: ReturnType<typeof setTimeout>
    if (countdown > 0) {
      timer = setTimeout(() => setCountdown(countdown - 1), 1000)
    }
    return () => clearTimeout(timer)
  }, [countdown])

  const form = useForm<z.infer<typeof formSchema>>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      email: '',
      password: '',
      confirmPassword: '',
      emailCode: '',
    },
  })

  async function handleSendCode() {
    const email = form.getValues('email')
    const result = z.string().email().safeParse(email)
    if (!result.success) {
      form.setError('email', { type: 'manual', message: t('Please enter a valid email.') })
      return
    }

    try {
      const res = await sendCode({ data: { email } })
      if (res.code === 200) {
        toast.success(t('Verification code sent successfully.'))
        setCountdown(60)
      } else {
        toast.error(res.message || t('Failed to send verification code.'))
      }
    } catch (error: any) {
      // handled globally
    }
  }

  async function onSubmit(data: z.infer<typeof formSchema>) {
    setIsLoading(true)

    try {
      const response = await register({
        data: {
          username: data.email,
          email: data.email,
          password: data.password,
          confirmPassword: data.confirmPassword,
          emailCode: data.emailCode,
        },
      })

      if (response.code === 200) {
        toast.success(`${t('Account created for')} ${data.email}. ${t('Please sign in.')}`)
        router.navigate({ to: '/sign-in' })
      } else {
        toast.error(response.message || t('Error creating account'))
      }
    } catch (error: any) {
      // toast is handled globally in handleServerError
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <Form {...form}>
      <form
        onSubmit={form.handleSubmit(onSubmit)}
        className={cn('grid gap-3', className)}
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
          name='password'
          render={({ field }) => (
            <FormItem>
              <FormLabel>{t('Password')}</FormLabel>
              <FormControl>
                <PasswordInput placeholder='********' {...field} />
              </FormControl>
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
                <PasswordInput placeholder='********' {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        <FormField
          control={form.control}
          name='emailCode'
          render={({ field }) => (
            <FormItem>
              <FormLabel>{t('Verification Code')}</FormLabel>
              <div className='flex gap-2'>
                <FormControl>
                  <Input placeholder='123456' {...field} />
                </FormControl>
                <Button
                  type='button'
                  variant='outline'
                  disabled={countdown > 0}
                  onClick={handleSendCode}
                >
                  {countdown > 0 ? `${countdown}s` : t('Send Code')}
                </Button>
              </div>
              <FormMessage />
            </FormItem>
          )}
        />
        <Button className='mt-2' disabled={isLoading}>
          {isLoading ? <Loader2 className='animate-spin' /> : <UserPlus />}
          {t('Create Account')}
        </Button>

        <div className='hidden'>
          <div className='relative my-2'>
            <div className='absolute inset-0 flex items-center'>
              <span className='w-full border-t' />
            </div>
            <div className='relative flex justify-center text-xs uppercase'>
              <span className='bg-background px-2 text-muted-foreground'>
                {t('Or continue with')}
              </span>
            </div>
          </div>

          <div className='grid grid-cols-2 gap-2'>
            <Button
              variant='outline'
              className='w-full'
              type='button'
              disabled={isLoading}
            >
              <IconGithub className='h-4 w-4' /> GitHub
            </Button>
            <Button
              variant='outline'
              className='w-full'
              type='button'
              disabled={isLoading}
            >
              <IconFacebook className='h-4 w-4' /> Facebook
            </Button>
          </div>
        </div>
      </form>
    </Form>
  )
}
