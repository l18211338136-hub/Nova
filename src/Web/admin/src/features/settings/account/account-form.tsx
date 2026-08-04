import { useEffect } from 'react'
import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { useForm } from 'react-hook-form'
import { CaretSortIcon, CheckIcon } from '@radix-ui/react-icons'
import { zodResolver } from '@hookform/resolvers/zod'
import { cn } from '@/lib/utils'
import { usePreferences, useSavePreferences } from '@/hooks/use-preferences'
import { Button } from '@/components/ui/button'
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from '@/components/ui/command'
import {
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover'
import { Skeleton } from '@/components/ui/skeleton'

/**
 * 目前项目只提供了 zh-CN / en-US 两套翻译文件，
 * 列出没有翻译的语言只会得到一个界面语言没变的假开关，所以这里只暴露真正支持的语言。
 */
const languages = [
  { label: '简体中文', value: 'zh-CN' },
  { label: 'English', value: 'en-US' },
] as const

/** 常用时区，覆盖国内与主要海外协作时区。 */
const timeZones = [
  'Asia/Shanghai',
  'Asia/Hong_Kong',
  'Asia/Taipei',
  'Asia/Tokyo',
  'Asia/Singapore',
  'Asia/Dubai',
  'Europe/London',
  'Europe/Berlin',
  'America/New_York',
  'America/Los_Angeles',
  'UTC',
] as const

const getAccountFormSchema = (t: (arg: string) => string) =>
  z.object({
    language: z.string().min(1, t('Please select a language.')),
    timeZone: z.string().min(1, t('Please select a timezone.')),
  })

type AccountFormValues = z.infer<ReturnType<typeof getAccountFormSchema>>

/** 浏览器时区可能不在候选列表里，此时回落到 UTC，避免出现选不中的空态。 */
function detectTimeZone(): string {
  const guess = Intl.DateTimeFormat().resolvedOptions().timeZone
  return timeZones.includes(guess as (typeof timeZones)[number]) ? guess : 'UTC'
}

export function AccountForm() {
  const { t, i18n } = useTranslation()
  const { preferences, isLoading } = usePreferences()
  const { save, isPending } = useSavePreferences({
    successMessage: t('Account settings updated.'),
  })

  const form = useForm<AccountFormValues>({
    resolver: zodResolver(getAccountFormSchema(t)),
    defaultValues: { language: i18n.language, timeZone: detectTimeZone() },
  })

  useEffect(() => {
    if (isLoading) return
    form.reset({
      language: preferences.language || i18n.language,
      timeZone: preferences.timeZone || detectTimeZone(),
    })
  }, [isLoading, preferences.language, preferences.timeZone, form, i18n.language])

  function onSubmit(values: AccountFormValues) {
    // 立即应用界面语言，不必等待请求返回
    if (values.language !== i18n.language) {
      i18n.changeLanguage(values.language)
      localStorage.setItem('i18nextLng', values.language)
    }
    save({ language: values.language, timeZone: values.timeZone })
  }

  if (isLoading) {
    return (
      <div className='space-y-6'>
        <Skeleton className='h-10 w-50' />
        <Skeleton className='h-10 w-50' />
      </div>
    )
  }

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className='space-y-8'>
        <FormField
          control={form.control}
          name='language'
          render={({ field }) => (
            <FormItem className='flex flex-col'>
              <FormLabel>{t('Language')}</FormLabel>
              <Popover>
                <PopoverTrigger asChild>
                  <FormControl>
                    <Button
                      variant='outline'
                      role='combobox'
                      className={cn(
                        'w-50 justify-between',
                        !field.value && 'text-muted-foreground'
                      )}
                    >
                      {languages.find((l) => l.value === field.value)?.label ??
                        t('Select language')}
                      <CaretSortIcon className='ms-2 h-4 w-4 shrink-0 opacity-50' />
                    </Button>
                  </FormControl>
                </PopoverTrigger>
                <PopoverContent className='w-50 p-0'>
                  <Command>
                    <CommandInput placeholder={t('Search language...')} />
                    <CommandEmpty>{t('No language found.')}</CommandEmpty>
                    <CommandGroup>
                      <CommandList>
                        {languages.map((language) => (
                          <CommandItem
                            value={language.label}
                            key={language.value}
                            onSelect={() =>
                              form.setValue('language', language.value, {
                                shouldDirty: true,
                              })
                            }
                          >
                            <CheckIcon
                              className={cn(
                                'size-4',
                                language.value === field.value
                                  ? 'opacity-100'
                                  : 'opacity-0'
                              )}
                            />
                            {language.label}
                          </CommandItem>
                        ))}
                      </CommandList>
                    </CommandGroup>
                  </Command>
                </PopoverContent>
              </Popover>
              <FormDescription>
                {t('This is the language that will be used in the dashboard.')}
              </FormDescription>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name='timeZone'
          render={({ field }) => (
            <FormItem className='flex flex-col'>
              <FormLabel>{t('Timezone')}</FormLabel>
              <Popover>
                <PopoverTrigger asChild>
                  <FormControl>
                    <Button
                      variant='outline'
                      role='combobox'
                      className={cn(
                        'w-64 justify-between',
                        !field.value && 'text-muted-foreground'
                      )}
                    >
                      {field.value || t('Select timezone')}
                      <CaretSortIcon className='ms-2 h-4 w-4 shrink-0 opacity-50' />
                    </Button>
                  </FormControl>
                </PopoverTrigger>
                <PopoverContent className='w-64 p-0'>
                  <Command>
                    <CommandInput placeholder={t('Search timezone...')} />
                    <CommandEmpty>{t('No timezone found.')}</CommandEmpty>
                    <CommandGroup>
                      <CommandList>
                        {timeZones.map((tz) => (
                          <CommandItem
                            value={tz}
                            key={tz}
                            onSelect={() =>
                              form.setValue('timeZone', tz, {
                                shouldDirty: true,
                              })
                            }
                          >
                            <CheckIcon
                              className={cn(
                                'size-4',
                                tz === field.value ? 'opacity-100' : 'opacity-0'
                              )}
                            />
                            {tz}
                          </CommandItem>
                        ))}
                      </CommandList>
                    </CommandGroup>
                  </Command>
                </PopoverContent>
              </Popover>
              <FormDescription>
                {t('Dates and times will be displayed in this timezone.')}
              </FormDescription>
              <FormMessage />
            </FormItem>
          )}
        />

        <Button type='submit' disabled={isPending}>
          {isPending ? t('Updating...') : t('Update account')}
        </Button>
      </form>
    </Form>
  )
}
