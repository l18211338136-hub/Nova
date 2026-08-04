import { useEffect } from 'react'
import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { useForm } from 'react-hook-form'
import { ChevronDownIcon } from '@radix-ui/react-icons'
import { zodResolver } from '@hookform/resolvers/zod'
import { fonts } from '@/config/fonts'
import { cn } from '@/lib/utils'
import { useFont } from '@/context/font-provider'
import { useTheme } from '@/context/theme-provider'
import { usePreferences, useSavePreferences } from '@/hooks/use-preferences'
import { Button, buttonVariants } from '@/components/ui/button'
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

const appearanceFormSchema = z.object({
  theme: z.enum(['light', 'dark', 'system']),
  font: z.enum(fonts),
})

type AppearanceFormValues = z.infer<typeof appearanceFormSchema>

export function AppearanceForm() {
  const { t } = useTranslation()
  const { font, setFont } = useFont()
  const { theme, setTheme } = useTheme()
  const { preferences, isLoading } = usePreferences()
  const { save, isPending } = useSavePreferences({
    successMessage: t('Appearance updated.'),
  })

  const form = useForm<AppearanceFormValues>({
    resolver: zodResolver(appearanceFormSchema),
    defaultValues: { theme: theme as AppearanceFormValues['theme'], font },
  })

  useEffect(() => {
    if (isLoading) return
    const nextFont =
      preferences.font && (fonts as readonly string[]).includes(preferences.font)
        ? preferences.font
        : font
    form.reset({
      theme: (preferences.theme as AppearanceFormValues['theme']) ?? theme,
      font: nextFont as AppearanceFormValues['font'],
    })
    // theme / font 是本地 provider 的当前值，仅作兜底，不参与依赖以免回填被本地态覆盖
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isLoading, preferences.theme, preferences.font])

  function onSubmit(data: AppearanceFormValues) {
    // 先本地生效（即时反馈），再落库同步到其他设备
    if (data.font !== font) setFont(data.font)
    if (data.theme !== theme) setTheme(data.theme)
    save({ theme: data.theme, font: data.font })
  }

  if (isLoading) {
    return (
      <div className='space-y-6'>
        <Skeleton className='h-10 w-50' />
        <Skeleton className='h-40 w-full max-w-md' />
      </div>
    )
  }

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className='space-y-8'>
        <FormField
          control={form.control}
          name='font'
          render={({ field }) => (
            <FormItem>
              <FormLabel>{t('Font')}</FormLabel>
              <div className='relative w-max'>
                <FormControl>
                  <select
                    className={cn(
                      buttonVariants({ variant: 'outline' }),
                      'w-50 appearance-none font-normal capitalize',
                      'dark:bg-background dark:hover:bg-background'
                    )}
                    {...field}
                  >
                    {fonts.map((f) => (
                      <option key={f} value={f}>
                        {t(f.charAt(0).toUpperCase() + f.slice(1))}
                      </option>
                    ))}
                  </select>
                </FormControl>
                <ChevronDownIcon className='absolute inset-e-3 top-2.5 h-4 w-4 opacity-50' />
              </div>
              <FormDescription className='font-manrope'>
                {t('Set the font you want to use in the dashboard.')}
              </FormDescription>
              <FormMessage />
            </FormItem>
          )}
        />
        <FormField
          control={form.control}
          name='theme'
          render={({ field }) => (
            <FormItem>
              <FormLabel>{t('Theme')}</FormLabel>
              <FormDescription>
                {t('Select the theme for the dashboard.')}
              </FormDescription>
              <FormMessage />
              <RadioGroup
                onValueChange={field.onChange}
                value={field.value}
                className='grid max-w-2xl grid-cols-2 gap-8 pt-2 md:grid-cols-3'
              >
                <FormItem>
                  <FormLabel className='[&:has([data-state=checked])>div]:border-primary'>
                    <FormControl>
                      <RadioGroupItem value='light' className='sr-only' />
                    </FormControl>
                    <div className='items-center rounded-md border-2 border-muted p-1 hover:border-accent'>
                      <div className='space-y-2 rounded-sm bg-[#ecedef] p-2'>
                        <div className='space-y-2 rounded-md bg-white p-2 shadow-xs'>
                          <div className='h-2 w-20 rounded-lg bg-[#ecedef]' />
                          <div className='h-2 w-25 rounded-lg bg-[#ecedef]' />
                        </div>
                        <div className='flex items-center space-x-2 rounded-md bg-white p-2 shadow-xs'>
                          <div className='h-4 w-4 rounded-full bg-[#ecedef]' />
                          <div className='h-2 w-25 rounded-lg bg-[#ecedef]' />
                        </div>
                        <div className='flex items-center space-x-2 rounded-md bg-white p-2 shadow-xs'>
                          <div className='h-4 w-4 rounded-full bg-[#ecedef]' />
                          <div className='h-2 w-25 rounded-lg bg-[#ecedef]' />
                        </div>
                      </div>
                    </div>
                    <span className='block w-full p-2 text-center font-normal'>
                      {t('Light')}
                    </span>
                  </FormLabel>
                </FormItem>
                <FormItem>
                  <FormLabel className='[&:has([data-state=checked])>div]:border-primary'>
                    <FormControl>
                      <RadioGroupItem value='dark' className='sr-only' />
                    </FormControl>
                    <div className='items-center rounded-md border-2 border-muted bg-popover p-1 hover:bg-accent hover:text-accent-foreground'>
                      <div className='space-y-2 rounded-sm bg-slate-950 p-2'>
                        <div className='space-y-2 rounded-md bg-slate-800 p-2 shadow-xs'>
                          <div className='h-2 w-20 rounded-lg bg-slate-400' />
                          <div className='h-2 w-25 rounded-lg bg-slate-400' />
                        </div>
                        <div className='flex items-center space-x-2 rounded-md bg-slate-800 p-2 shadow-xs'>
                          <div className='h-4 w-4 rounded-full bg-slate-400' />
                          <div className='h-2 w-25 rounded-lg bg-slate-400' />
                        </div>
                        <div className='flex items-center space-x-2 rounded-md bg-slate-800 p-2 shadow-xs'>
                          <div className='h-4 w-4 rounded-full bg-slate-400' />
                          <div className='h-2 w-25 rounded-lg bg-slate-400' />
                        </div>
                      </div>
                    </div>
                    <span className='block w-full p-2 text-center font-normal'>
                      {t('Dark')}
                    </span>
                  </FormLabel>
                </FormItem>
                <FormItem>
                  <FormLabel className='[&:has([data-state=checked])>div]:border-primary'>
                    <FormControl>
                      <RadioGroupItem value='system' className='sr-only' />
                    </FormControl>
                    <div className='items-center rounded-md border-2 border-muted p-1 hover:border-accent'>
                      <div className='space-y-2 rounded-sm bg-gradient-to-r from-[#ecedef] to-slate-950 p-2'>
                        <div className='space-y-2 rounded-md bg-gradient-to-r from-white to-slate-800 p-2 shadow-xs'>
                          <div className='h-2 w-20 rounded-lg bg-gradient-to-r from-[#ecedef] to-slate-400' />
                          <div className='h-2 w-25 rounded-lg bg-gradient-to-r from-[#ecedef] to-slate-400' />
                        </div>
                        <div className='flex items-center space-x-2 rounded-md bg-gradient-to-r from-white to-slate-800 p-2 shadow-xs'>
                          <div className='h-4 w-4 rounded-full bg-gradient-to-r from-[#ecedef] to-slate-400' />
                          <div className='h-2 w-25 rounded-lg bg-gradient-to-r from-[#ecedef] to-slate-400' />
                        </div>
                        <div className='flex items-center space-x-2 rounded-md bg-gradient-to-r from-white to-slate-800 p-2 shadow-xs'>
                          <div className='h-4 w-4 rounded-full bg-gradient-to-r from-[#ecedef] to-slate-400' />
                          <div className='h-2 w-25 rounded-lg bg-gradient-to-r from-[#ecedef] to-slate-400' />
                        </div>
                      </div>
                    </div>
                    <span className='block w-full p-2 text-center font-normal'>
                      {t('System')}
                    </span>
                  </FormLabel>
                </FormItem>
              </RadioGroup>
            </FormItem>
          )}
        />

        <Button type='submit' disabled={isPending}>
          {isPending ? t('Updating...') : t('Update preferences')}
        </Button>
      </form>
    </Form>
  )
}
