import { z } from 'zod'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { toast } from 'sonner'
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
import { MenuDto } from './menus-provider'
import { useTranslation } from 'react-i18next'
import { useCreateMenu, useUpdateMenu, getMenusQueryKey } from '@/api/endpoints/menus'
import { useQueryClient } from '@tanstack/react-query'

const getFormSchema = (t: (arg: string) => string) => z.object({
  name: z.string().min(1, { message: t('Name is required.') }),
  path: z.string().min(1, { message: t('Path is required.') }),
  component: z.string().min(1, { message: t('Component is required.') }),
  icon: z.string().nullable().optional(),
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

  const form = useForm<MenusForm>({
    resolver: zodResolver(formSchema),
    defaultValues: isEdit ? {
      name: currentRow?.name ?? '',
      path: currentRow?.path ?? '',
      component: currentRow?.component ?? '',
      icon: currentRow?.icon ?? '',
      sort: currentRow?.sort ?? 0,
      isEnabled: currentRow?.isEnabled ?? true,
      remarks: currentRow?.remarks ?? '',
    } : {
      name: '',
      path: '',
      component: '',
      icon: '',
      sort: 0,
      isEnabled: true,
      remarks: '',
    },
  })

  const onSubmit = (values: MenusForm) => {
    if (isEdit && currentRow) {
      updateMenuMutation.mutate(
        {
          id: currentRow.id!,
          data: {
            id: currentRow.id!,
            parentId: currentRow.parentId,
            ...values,
          }
        },
        {
          onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: getMenusQueryKey() })
            form.reset()
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
            parentId: isSubMenu ? currentRow?.id : null,
            ...values,
          }
        },
        {
          onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: getMenusQueryKey() })
            form.reset()
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
      onOpenChange={(state) => {
        form.reset()
        onOpenChange(state)
      }}
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
                    <FormControl>
                      <Input placeholder='e.g. Users' className='col-span-4' {...field} value={field.value || ''} />
                    </FormControl>
                    <FormMessage className='col-span-4 col-start-3' />
                  </FormItem>
                )}
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
