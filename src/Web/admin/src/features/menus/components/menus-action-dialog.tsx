import { useState, useEffect, useMemo } from 'react'
import { z } from 'zod'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { toast } from 'sonner'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Switch } from '@/components/ui/switch'
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover'
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from '@/components/ui/command'
import * as Icons from 'lucide-react'
import { MenuDto } from './menus-provider'
import { useTranslation } from 'react-i18next'
import { useCreateMenu, useUpdateMenu, useMenus, getMenusQueryKey, getGetMyMenusQueryKey } from '@/api/endpoints/menus'
import { useQueryClient } from '@tanstack/react-query'

const predefinedIcons = [
  // 核心导航/页面
  'LayoutDashboard', 'Home', 'Compass', 'Layout', 'Layers', 'LayoutGrid', 'Grid', 'Menu', 'MoreHorizontal', 'MoreVertical',
  // 系统设置/权限
  'Settings', 'Sliders', 'Wrench', 'Tool', 'Shield', 'ShieldCheck', 'ShieldAlert', 'Lock', 'Unlock', 'Key', 
  // 用户/角色
  'Users', 'User', 'UserCheck', 'UserPlus', 'UserMinus', 'UserCog', 'Badge', 'Building', 'Briefcase', 'Contact',
  // 财务/数据/统计
  'BarChart', 'PieChart', 'LineChart', 'TrendingUp', 'TrendingDown', 'Activity', 'DollarSign', 'CreditCard', 'Wallet', 'Banknote', 'Receipt',
  // 文档/列表
  'List', 'ListTodo', 'ListOrdered', 'FileText', 'File', 'Files', 'Folder', 'FolderOpen', 'Archive', 'Clipboard', 'ClipboardList',
  // 电商/产品
  'ShoppingCart', 'ShoppingBag', 'ShoppingBasket', 'Tag', 'Package', 'Box', 'Truck', 'Gift',
  // 通讯/消息
  'Mail', 'MessageSquare', 'MessageCircle', 'Bell', 'BellRing', 'Send', 'Share', 'Share2',
  // 媒体/设备
  'Image', 'Camera', 'Video', 'Mic', 'Headphones', 'Monitor', 'Smartphone', 'Laptop', 'Tablet', 'HardDrive', 'Server',
  // 时间/日期
  'Calendar', 'Clock', 'History', 'Timer',
  // 状态/其他
  'CheckCircle', 'XCircle', 'AlertCircle', 'Info', 'HelpCircle', 'Star', 'Heart', 'Bookmark', 'Flag', 'MapPin', 'Globe', 'Cloud', 'Zap', 'Lightbulb', 'Search'
]

const getFormSchema = (t: (arg: string) => string) => z.object({
  name: z.string().min(1, { message: t('Name is required.') }),
  path: z.string().min(1, { message: t('Path is required.') }),
  component: z.string().min(1, { message: t('Component is required.') }),
  icon: z.string().nullable().optional(),
  parentId: z.string().nullable().optional(),
  sort: z.number().min(0),
  isEnabled: z.boolean(),
  remarks: z.string().nullable().optional(),
})

type MenusForm = z.infer<ReturnType<typeof getFormSchema>>

interface Props {
  currentRow?: MenuDto | null
  open: boolean
  onOpenChange: (open: boolean) => void
  isEdit?: boolean
  isSubMenu?: boolean
}

export function MenusActionDialog({ currentRow, open, onOpenChange, isEdit, isSubMenu }: Props) {
  const { t } = useTranslation()
  const formSchema = getFormSchema(t)
  const queryClient = useQueryClient()
  const createMenuMutation = useCreateMenu()
  const updateMenuMutation = useUpdateMenu()
  
  const [iconOpen, setIconOpen] = useState(false)
  const [parentOpen, setParentOpen] = useState(false)

  const { data: menusData } = useMenus()
  const allMenus = (menusData?.data?.items || []) as MenuDto[]

  // 编辑时不能把自己或自己的后代选为父节点，避免循环引用
  const descendantIds = useMemo(() => {
    if (!isEdit || !currentRow?.id) return new Set<string>()
    const ids = new Set<string>()
    const collect = (parentId: string) => {
      allMenus
        .filter((m) => m.parentId === parentId)
        .forEach((child) => {
          if (child.id) {
            ids.add(child.id)
            collect(child.id)
          }
        })
    }
    collect(currentRow.id)
    return ids
  }, [allMenus, currentRow, isEdit])

  const parentCandidates = useMemo(() => {
    return allMenus.filter(
      (m) => m.id && m.id !== currentRow?.id && !descendantIds.has(m.id)
    )
  }, [allMenus, currentRow, descendantIds])

  const form = useForm<MenusForm>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      name: '',
      path: '',
      component: '',
      icon: '',
      parentId: null,
      sort: 0,
      isEnabled: true,
      remarks: '',
    },
  })

  useEffect(() => {
    if (open) {
      form.reset(isEdit ? {
        name: currentRow?.name ?? '',
        path: currentRow?.path ?? '',
        component: currentRow?.component ?? '',
        icon: currentRow?.icon ?? '',
        parentId: currentRow?.parentId ?? null,
        sort: currentRow?.sort ?? 0,
        isEnabled: currentRow?.isEnabled ?? true,
        remarks: currentRow?.remarks ?? '',
      } : isSubMenu ? {
        name: '',
        path: '',
        component: '',
        icon: '',
        parentId: currentRow?.id ?? null,
        sort: 0,
        isEnabled: true,
        remarks: '',
      } : {
        name: '',
        path: '',
        component: '',
        icon: '',
        parentId: null,
        sort: 0,
        isEnabled: true,
        remarks: '',
      })
    }
  }, [open, isEdit, isSubMenu, currentRow, form])

  const onSubmit = (values: MenusForm) => {
    if (isEdit && currentRow) {
      updateMenuMutation.mutate(
        {
          id: currentRow.id!,
          data: {
            id: currentRow.id!,
            ...values,
          }
        },
        {
          onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: getMenusQueryKey() })
            queryClient.invalidateQueries({ queryKey: getGetMyMenusQueryKey() })
            toast.success(t('Menu updated successfully'))
            onOpenChange(false)
          },
          onError: (error: any) => {
            toast.error(error.response?.data?.message || t('Failed to update menu'))
          }
        }
      )
    } else {
      createMenuMutation.mutate(
        {
          data: {
            ...values,
          }
        },
        {
          onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: getMenusQueryKey() })
            queryClient.invalidateQueries({ queryKey: getGetMyMenusQueryKey() })
            toast.success(t('Menu created successfully'))
            onOpenChange(false)
          },
          onError: (error: any) => {
            toast.error(error.response?.data?.message || t('Failed to create menu'))
          }
        }
      )
    }
  }

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
    >
      <DialogContent className='sm:max-w-lg'>
        <DialogHeader className='text-left'>
          <DialogTitle>
            {isEdit ? t('Edit Menu') : isSubMenu ? t('Add Child Menu') : t('Create Menu')}
          </DialogTitle>
          <DialogDescription>
            {isEdit
              ? t('Update the menu details below.')
              : t('Fill in the details below to create a new menu.')}
          </DialogDescription>
        </DialogHeader>
        <div className='scroll-thin -mr-1 max-h-[60vh] overflow-y-auto pr-1'>
          <Form {...form}>
            <form id='menu-form' onSubmit={form.handleSubmit(onSubmit)} className='space-y-4 p-0.5'>
              <FormField
                control={form.control}
                name='name'
                render={({ field }) => (
                  <FormItem className='grid grid-cols-6 items-center space-y-0 gap-x-4 gap-y-1'>
                    <FormLabel className='col-span-2 text-end'>{t('Name')}</FormLabel>
                    <FormControl>
                      <Input placeholder='e.g. User Management' className='col-span-4' {...field} />
                    </FormControl>
                    <FormMessage className='col-span-4 col-start-3' />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name='path'
                render={({ field }) => (
                  <FormItem className='grid grid-cols-6 items-center space-y-0 gap-x-4 gap-y-1'>
                    <FormLabel className='col-span-2 text-end'>{t('Path')}</FormLabel>
                    <FormControl>
                      <Input placeholder='e.g. /users' className='col-span-4' {...field} />
                    </FormControl>
                    <FormMessage className='col-span-4 col-start-3' />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name='component'
                render={({ field }) => (
                  <FormItem className='grid grid-cols-6 items-center space-y-0 gap-x-4 gap-y-1'>
                    <FormLabel className='col-span-2 text-end'>{t('Component')}</FormLabel>
                    <FormControl>
                      <Input placeholder='e.g. @/features/users' className='col-span-4' {...field} />
                    </FormControl>
                    <FormMessage className='col-span-4 col-start-3' />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name='icon'
                render={({ field }) => (
                  <FormItem className='grid grid-cols-6 items-center space-y-0 gap-x-4 gap-y-1'>
                    <FormLabel className='col-span-2 text-end'>{t('Icon')}</FormLabel>
                    <Popover open={iconOpen} onOpenChange={setIconOpen}>
                      <PopoverTrigger asChild>
                        <FormControl>
                          <Button
                            variant='outline'
                            role='combobox'
                            aria-expanded={iconOpen}
                            className={cn(
                              'col-span-4 w-full justify-between',
                              !field.value && 'text-muted-foreground'
                            )}
                          >
                            {field.value ? (
                              <div className='flex items-center gap-2'>
                                {(() => {
                                  const IconComp = (Icons as any)[field.value]
                                  return IconComp ? <IconComp className='h-4 w-4' /> : null
                                })()}
                                <span>{field.value}</span>
                              </div>
                            ) : (
                              t('Select an icon')
                            )}
                            <Icons.ChevronsUpDown className='ms-2 h-4 w-4 shrink-0 opacity-50' />
                          </Button>
                        </FormControl>
                      </PopoverTrigger>
                      <PopoverContent className='w-[280px] p-0' align='start'>
                        <Command>
                          <CommandInput placeholder={t('Search icon...')} />
                          <CommandList>
                            <CommandEmpty>{t('No icon found.')}</CommandEmpty>
                            <CommandGroup>
                              {predefinedIcons.map((iconName) => {
                                const IconComp = (Icons as any)[iconName]
                                return (
                                  <CommandItem
                                    value={iconName}
                                    key={iconName}
                                    onSelect={() => {
                                      form.setValue('icon', iconName)
                                      setIconOpen(false)
                                    }}
                                  >
                                    <div className='flex items-center gap-2'>
                                      {IconComp && <IconComp className='h-4 w-4' />}
                                      <span>{iconName}</span>
                                    </div>
                                    <Icons.CheckIcon
                                      className={cn(
                                        'ms-auto h-4 w-4',
                                        iconName === field.value ? 'opacity-100' : 'opacity-0'
                                      )}
                                    />
                                  </CommandItem>
                                )
                              })}
                            </CommandGroup>
                          </CommandList>
                        </Command>
                      </PopoverContent>
                    </Popover>
                    <FormMessage className='col-span-4 col-start-3' />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name='parentId'
                render={({ field }) => {
                  const selectedParent = allMenus.find((m) => m.id === field.value)
                  return (
                    <FormItem className='grid grid-cols-6 items-center space-y-0 gap-x-4 gap-y-1'>
                      <FormLabel className='col-span-2 text-end'>{t('Parent Menu')}</FormLabel>
                      <Popover open={parentOpen} onOpenChange={setParentOpen}>
                        <PopoverTrigger asChild>
                          <FormControl>
                            <Button
                              variant='outline'
                              role='combobox'
                              aria-expanded={parentOpen}
                              className={cn(
                                'col-span-4 w-full justify-between',
                                !field.value && 'text-muted-foreground'
                              )}
                            >
                              {selectedParent ? (
                                <span>{selectedParent.name}</span>
                              ) : (
                                t('No parent (top-level menu)')
                              )}
                              <Icons.ChevronsUpDown className='ms-2 h-4 w-4 shrink-0 opacity-50' />
                            </Button>
                          </FormControl>
                        </PopoverTrigger>
                        <PopoverContent className='w-[280px] p-0' align='start'>
                          <Command>
                            <CommandInput placeholder={t('Search parent menu...')} />
                            <CommandList>
                              <CommandEmpty>{t('No menu found.')}</CommandEmpty>
                              <CommandGroup>
                                <CommandItem
                                  value='__root__'
                                  onSelect={() => {
                                    form.setValue('parentId', null)
                                    setParentOpen(false)
                                  }}
                                >
                                  <span>{t('No parent (top-level menu)')}</span>
                                  <Icons.CheckIcon
                                    className={cn(
                                      'ms-auto h-4 w-4',
                                      !field.value ? 'opacity-100' : 'opacity-0'
                                    )}
                                  />
                                </CommandItem>
                                {parentCandidates.map((menu) => (
                                  <CommandItem
                                    value={menu.name || menu.id}
                                    key={menu.id}
                                    onSelect={() => {
                                      form.setValue('parentId', menu.id!)
                                      setParentOpen(false)
                                    }}
                                  >
                                    <span>{menu.name}</span>
                                    <Icons.CheckIcon
                                      className={cn(
                                        'ms-auto h-4 w-4',
                                        menu.id === field.value ? 'opacity-100' : 'opacity-0'
                                      )}
                                    />
                                  </CommandItem>
                                ))}
                              </CommandGroup>
                            </CommandList>
                          </Command>
                        </PopoverContent>
                      </Popover>
                      <FormMessage className='col-span-4 col-start-3' />
                    </FormItem>
                  )
                }}
              />
              <FormField
                control={form.control}
                name='sort'
                render={({ field }) => (
                  <FormItem className='grid grid-cols-6 items-center space-y-0 gap-x-4 gap-y-1'>
                    <FormLabel className='col-span-2 text-end'>{t('Sort')}</FormLabel>
                    <FormControl>
                      <Input 
                        type='number' 
                        className='col-span-4' 
                        {...field} 
                        onChange={(e) => field.onChange(Number(e.target.value))}
                      />
                    </FormControl>
                    <FormMessage className='col-span-4 col-start-3' />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name='remarks'
                render={({ field }) => (
                  <FormItem className='grid grid-cols-6 items-center space-y-0 gap-x-4 gap-y-1'>
                    <FormLabel className='col-span-2 text-end'>{t('Remarks')}</FormLabel>
                    <FormControl>
                      <Textarea placeholder='...' className='col-span-4 resize-none' {...field} value={field.value || ''} />
                    </FormControl>
                    <FormMessage className='col-span-4 col-start-3' />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name='isEnabled'
                render={({ field }) => (
                  <FormItem className='grid grid-cols-6 items-center space-y-0 gap-x-4 gap-y-1'>
                    <FormLabel className='col-span-2 text-end'>{t('Status')}</FormLabel>
                    <FormControl>
                      <div className='col-span-4 flex items-center h-10'>
                        <Switch
                          checked={field.value}
                          onCheckedChange={field.onChange}
                        />
                        <span className='ms-2 text-sm text-muted-foreground'>
                          {field.value ? t('Active') : t('Inactive')}
                        </span>
                      </div>
                    </FormControl>
                    <FormMessage className='col-span-4 col-start-3' />
                  </FormItem>
                )}
              />
            </form>
          </Form>
        </div>
        <DialogFooter>
          <Button type='button' variant='outline' onClick={() => onOpenChange(false)}>
            {t('Cancel')}
          </Button>
          <Button type='submit' form='menu-form'>
            {t('Save changes')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
