import { useEffect, useMemo } from 'react'
import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useSavePreferences } from '@/hooks/use-preferences'
import { useSidebarNav } from '@/hooks/use-sidebar-nav'
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
import { Skeleton } from '@/components/ui/skeleton'

const getDisplayFormSchema = (t: (arg: string) => string) =>
  z.object({
    // 存的是「显示的入口」，提交时再换算成隐藏项
    visible: z.array(z.string()).refine((value) => value.length > 0, {
      message: t('You have to keep at least one sidebar item visible.'),
    }),
  })

type DisplayFormValues = z.infer<ReturnType<typeof getDisplayFormSchema>>

export function DisplayForm() {
  const { t } = useTranslation()
  const { allGroups, options, hiddenItems, isLoading } = useSidebarNav()
  const { save, isPending } = useSavePreferences({
    successMessage: t('Sidebar updated.'),
  })

  const allUrls = useMemo(() => options.map((o) => o.url), [options])

  const form = useForm<DisplayFormValues>({
    resolver: zodResolver(getDisplayFormSchema(t)),
    defaultValues: { visible: [] },
  })

  useEffect(() => {
    if (isLoading) return
    form.reset({ visible: allUrls.filter((url) => !hiddenItems.has(url)) })
  }, [isLoading, allUrls, hiddenItems, form])

  function onSubmit(values: DisplayFormValues) {
    // 后端存的是隐藏项而非显示项：这样以后新增的菜单默认可见，无需回填历史数据
    const hidden = allUrls.filter((url) => !values.visible.includes(url))
    save({ hiddenSidebarItems: hidden })
  }

  if (isLoading) {
    return (
      <div className='space-y-4'>
        <Skeleton className='h-6 w-40' />
        <Skeleton className='h-5 w-full max-w-sm' />
        <Skeleton className='h-5 w-full max-w-sm' />
        <Skeleton className='h-5 w-full max-w-sm' />
      </div>
    )
  }

  if (options.length === 0) {
    return (
      <p className='text-sm text-muted-foreground'>
        {t('No sidebar items available.')}
      </p>
    )
  }

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className='space-y-8'>
        <FormField
          control={form.control}
          name='visible'
          render={() => (
            <FormItem>
              <div className='mb-4'>
                <FormLabel className='text-base'>{t('Sidebar')}</FormLabel>
                <FormDescription>
                  {t('Select the items you want to display in the sidebar.')}
                </FormDescription>
              </div>

              <div className='space-y-6'>
                {allGroups.map((group) => {
                  const groupOptions = options.filter(
                    (o) => o.group === group.title
                  )
                  if (groupOptions.length === 0) return null

                  return (
                    <div key={group.title} className='space-y-2'>
                      <p className='text-sm font-medium text-muted-foreground'>
                        {group.title}
                      </p>
                      {groupOptions.map((option) => (
                        <FormField
                          key={option.url}
                          control={form.control}
                          name='visible'
                          render={({ field }) => (
                            <FormItem className='flex flex-row items-start'>
                              <FormControl>
                                <Checkbox
                                  checked={field.value?.includes(option.url)}
                                  onCheckedChange={(checked) =>
                                    field.onChange(
                                      checked
                                        ? [...field.value, option.url]
                                        : field.value.filter(
                                            (v) => v !== option.url
                                          )
                                    )
                                  }
                                />
                              </FormControl>
                              <FormLabel className='font-normal'>
                                {option.title}
                                <span className='ms-2 text-xs text-muted-foreground'>
                                  {option.url}
                                </span>
                              </FormLabel>
                            </FormItem>
                          )}
                        />
                      ))}
                    </div>
                  )
                })}
              </div>

              <FormMessage />
            </FormItem>
          )}
        />
        <Button type='submit' disabled={isPending}>
          {isPending ? t('Updating...') : t('Update display')}
        </Button>
      </form>
    </Form>
  )
}
