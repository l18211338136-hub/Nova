import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Link, useNavigate } from '@tanstack/react-router'
import { Loader2, LogIn } from 'lucide-react'
import { toast } from 'sonner'
import { useAuthStore } from '@/stores/auth-store'
import { cn } from '@/lib/utils'
import { IconFacebook, IconGithub } from '@/assets/brand-icons'
import { useLogin, useEmailLogin, useSendEmailLoginCode, resolveTenant } from '@/api/endpoints/auth'
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
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'

const getPasswordFormSchema = (t: (arg: string) => string) => z.object({
  account: z
    .string()
    .min(1, t('Please enter your username, email or phone.')),
  password: z
    .string()
    .min(1, t('Please enter your password.'))
    .min(7, t('Password must be at least 7 characters long.')),
})

const getCodeFormSchema = (t: (arg: string) => string) => z.object({
  email: z.email({
    error: (iss) => (iss.input === '' ? t('Please enter your email.') : undefined),
  }),
  emailCode: z.string().min(1, t('Please enter the verification code.')),
})

interface UserAuthFormProps extends React.HTMLAttributes<HTMLDivElement> {
  redirectTo?: string
}

export function UserAuthForm({
  className,
  redirectTo,
  ...props
}: UserAuthFormProps) {
  const { t } = useTranslation()
  const passwordFormSchema = getPasswordFormSchema(t)
  const codeFormSchema = getCodeFormSchema(t)

  const [isLoading, setIsLoading] = useState(false)
  const [isSendingCode, setIsSendingCode] = useState(false)
  const [countdown, setCountdown] = useState(0)

  const navigate = useNavigate()
  const { auth } = useAuthStore()

  const { mutateAsync: login } = useLogin()
  const { mutateAsync: emailLogin } = useEmailLogin()
  const { mutateAsync: sendCode } = useSendEmailLoginCode()

  useEffect(() => {
    let timer: ReturnType<typeof setTimeout>
    if (countdown > 0) {
      timer = setTimeout(() => setCountdown(countdown - 1), 1000)
    }
    return () => clearTimeout(timer)
  }, [countdown])

  const passwordForm = useForm<z.infer<typeof passwordFormSchema>>({
    resolver: zodResolver(passwordFormSchema),
    defaultValues: {
      account: '',
      password: '',
    },
  })

  const codeForm = useForm<z.infer<typeof codeFormSchema>>({
    resolver: zodResolver(codeFormSchema),
    defaultValues: {
      email: '',
      emailCode: '',
    },
  })

  async function handleSendCode() {
    const email = codeForm.getValues('email')
    const result = z.string().email().safeParse(email)
    if (!result.success) {
      codeForm.setError('email', { type: 'manual', message: t('Please enter a valid email.') })
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
      // handled globally
    } finally {
      setIsSendingCode(false)
    }
  }

  async function onSubmitPassword(data: z.infer<typeof passwordFormSchema>) {
    setIsLoading(true)
    try {
      const response = await login({
        data: { account: data.account, password: data.password }
      })
      handleLoginSuccess(response)
    } catch (error: any) {
    } finally {
      setIsLoading(false)
    }
  }

  async function onSubmitCode(data: z.infer<typeof codeFormSchema>) {
    setIsLoading(true)
    try {
      const response = await emailLogin({
        data: { email: data.email, code: data.emailCode }
      })
      handleLoginSuccess(response)
    } catch (error: any) {
    } finally {
      setIsLoading(false)
    }
  }

  function handleLoginSuccess(response: any) {
    const resData = response.data
    if (response.code === 200 && resData && resData.token) {
      auth.setAccessToken(resData.token)
      if (resData.refreshToken) {
        auth.setRefreshToken(resData.refreshToken)
      }
      toast.success(`${t('Welcome back')}!`)
      // 强制跳转到首页，忽略之前的重定向路径，避免登录后跳入特定管理页面
      navigate({ to: '/', replace: true })
    } else {
      toast.error(response.message || t('Error signing in'))
    }
  }

  return (
    <div className={cn('grid gap-6', className)} {...props}>
      <Tabs defaultValue="password">
        <TabsList className="grid w-full grid-cols-2">
          <TabsTrigger value="password">{t('Password Login')}</TabsTrigger>
          <TabsTrigger value="code">{t('Code Login')}</TabsTrigger>
        </TabsList>
        <TabsContent value="password">
          <Form {...passwordForm}>
            <form onSubmit={passwordForm.handleSubmit(onSubmitPassword)} className='grid gap-3 pt-4'>
              <FormField
                control={passwordForm.control}
                name='account'
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('Username / Email / Phone')}</FormLabel>
                    <FormControl>
                      <Input placeholder={t('root or name@example.com or phone')} {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={passwordForm.control}
                name='password'
                render={({ field }) => (
                  <FormItem className='relative'>
                    <FormLabel>{t('Password')}</FormLabel>
                    <FormControl>
                      <PasswordInput placeholder='********' {...field} />
                    </FormControl>
                    <FormMessage />
                    <Link
                      to='/forgot-password'
                      className='absolute inset-e-0 -top-0.5 text-sm font-medium text-muted-foreground hover:opacity-75'
                    >
                      {t('Forgot password?')}
                    </Link>
                  </FormItem>
                )}
              />
              <Button className='mt-2' disabled={isLoading}>
                {isLoading ? <Loader2 className='animate-spin' /> : <LogIn />}
                {t('Sign In')}
              </Button>
            </form>
          </Form>
        </TabsContent>
        <TabsContent value="code">
          <Form {...codeForm}>
            <form onSubmit={codeForm.handleSubmit(onSubmitCode)} className='grid gap-3 pt-4'>
              <FormField
                control={codeForm.control}
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
                control={codeForm.control}
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
                        disabled={countdown > 0 || isSendingCode}
                        onClick={handleSendCode}
                      >
                        {countdown > 0 ? `${countdown}s` : (isSendingCode ? <Loader2 className="h-4 w-4 animate-spin" /> : t('Send Code'))}
                      </Button>
                    </div>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <Button className='mt-2' disabled={isLoading}>
                {isLoading ? <Loader2 className='animate-spin' /> : <LogIn />}
                {t('Sign In')}
              </Button>
            </form>
          </Form>
        </TabsContent>
      </Tabs>

      {/* 暂时隐藏的第三方登录按钮，等后端接入 OAuth 后去掉 hidden 即可恢复 */}
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
          <Button variant='outline' type='button' disabled={isLoading}>
            <IconGithub className='h-4 w-4' /> {t('GitHub')}
          </Button>
          <Button variant='outline' type='button' disabled={isLoading}>
            <IconFacebook className='h-4 w-4' /> {t('Facebook')}
          </Button>
        </div>
      </div>
    </div>
  )
}
