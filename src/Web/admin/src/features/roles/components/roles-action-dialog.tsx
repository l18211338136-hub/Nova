import { z } from 'zod'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'
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
import { Switch } from '@/components/ui/switch'
import { Textarea } from '@/components/ui/textarea'
import { type RoleDto as Role } from '@/api/model'
import { useCreateRole, useUpdateRole } from '@/api/endpoints/roles'
import { useQueryClient } from '@tanstack/react-query'
import React from 'react'



const ACTION_MAP: Record<string, string> = {
  'Read': '查询',
  'Create': '新增',
  'Update': '编辑',
  'Delete': '删除',
}

const getFormSchema = (t: (arg: string) => string) => z.object({
  name: z.string().min(1, { message: t('Name is required.') }),
  displayName: z.string().min(1, { message: t('Display Name is required.') }),
  remarks: z.string().nullable().optional(),
  sort: z.number(),
  isEnabled: z.boolean(),
  isEnabled: z.boolean(),
})

type RoleForm = z.infer<ReturnType<typeof getFormSchema>>

interface Props {
  currentRow?: Role
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function RolesActionDialog({ currentRow, open, onOpenChange }: Props) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const isEdit = !!currentRow

  const formSchema = getFormSchema(t)

  const form = useForm<RoleForm>({
    resolver: zodResolver(formSchema),
    defaultValues: isEdit
      ? {
        name: currentRow.name ?? '',
        displayName: currentRow.displayName ?? '',
        remarks: currentRow.remarks,
        sort: currentRow.sort ?? 0,
        isEnabled: currentRow.isEnabled ?? true,
      }
      : {
        name: '',
        displayName: '',
        remarks: '',
        sort: 0,
        isEnabled: true,
      },
  })

  const createMutation = useCreateRole({
    mutation: {
      onSuccess: () => {
        toast.success(t('Role created successfully'))
        queryClient.invalidateQueries({ queryKey: ['roles'] })
        onOpenChange(false)
        form.reset()
      },
      onError: (error: any) => {
        toast.error(t('Failed to create role'), {
          description: error?.response?.data?.title || error.message,
        })
      }
    }
  })

  const updateMutation = useUpdateRole({
    mutation: {
      onSuccess: () => {
        toast.success(t('Role updated successfully'))
        queryClient.invalidateQueries({ queryKey: ['roles'] })
        onOpenChange(false)
      },
      onError: (error: any) => {
        toast.error(t('Failed to update role'), {
          description: error?.response?.data?.title || error.message,
        })
      }
    }
  })

  const onSubmit = (values: RoleForm) => {
    if (isEdit && currentRow?.id) {
      updateMutation.mutate({
        id: currentRow.id,
        data: {
          ...values,
        },
      })
    } else {
      createMutation.mutate({
        data: {
          ...values,
        },
      })
    }
  }

  const isPending = createMutation.isPending || updateMutation.isPending

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
          <DialogTitle>{isEdit ? t('Edit Role') : t('Create Role')}</DialogTitle>
          <DialogDescription>
            {isEdit ? t('Update the role details below.') : t('Fill in the details below to create a new role.')}
          </DialogDescription>
        </DialogHeader>
        <div className='scroll-thin -mr-1 max-h-[60vh] overflow-y-auto pr-1'>
          <Form {...form}>
            <form id='role-form' onSubmit={form.handleSubmit(onSubmit)} className='space-y-4 p-0.5'>
              <FormField
                control={form.control}
                name='name'
                render={({ field }) => (
                  <FormItem className='grid grid-cols-6 items-center space-y-0 gap-x-4 gap-y-1'>
                    <FormLabel className='col-span-2 text-end'>{t('Name')}</FormLabel>
                    <FormControl>
                      <Input
                        placeholder='admin'
                        className='col-span-4'
                        disabled={isEdit}
                        {...field}
                      />
                    </FormControl>
                    <FormMessage className='col-span-4 col-start-3' />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name='displayName'
                render={({ field }) => (
                  <FormItem className='grid grid-cols-6 items-center space-y-0 gap-x-4 gap-y-1'>
                    <FormLabel className='col-span-2 text-end'>{t('Display Name')}</FormLabel>
                    <FormControl>
                      <Input
                        placeholder='Administrator'
                        className='col-span-4'
                        {...field}
                      />
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
                        type="number"
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
                      <Textarea
                        placeholder='...'
                        className='col-span-4 resize-none'
                        {...field}
                        value={field.value || ''}
                      />
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
          <Button type='submit' form='role-form' disabled={isPending}>
            {t('Save changes')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
