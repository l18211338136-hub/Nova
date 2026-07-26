import { z } from 'zod'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useTranslation } from 'react-i18next'
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
import { Switch } from '@/components/ui/switch'
import { TenantDto as Tenant } from '@/api/model'
import { useCreateTenant, useUpdateTenant } from '@/api/endpoints/tenants'
import { useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { format } from 'date-fns'

interface TenantsActionDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  currentRow?: Tenant | null
}

const getFormSchema = (t: (arg: string) => string) => z.object({
  id: z.string().min(1, t('Tenant ID is required.')),
  name: z.string().min(1, t('Name is required.')),
  identifier: z.string().optional(),
  adminEmail: z.string().email(t('Invalid email address.')),
  connectionString: z.string().optional(),
  issuer: z.string().optional(),
  isActive: z.boolean(),
  validUpto: z.string().min(1, t('Valid Upto is required.')),
})

type TenantFormValues = z.infer<ReturnType<typeof getFormSchema>>

export function TenantsActionDialog({ open, onOpenChange, currentRow }: TenantsActionDialogProps) {
  const { t } = useTranslation()
  const isEdit = !!currentRow
  const queryClient = useQueryClient()

  const { mutateAsync: createTenant, isPending: isCreating } = useCreateTenant()
  const { mutateAsync: updateTenant, isPending: isUpdating } = useUpdateTenant()

  const defaultValidUpto = new Date()
  defaultValidUpto.setFullYear(defaultValidUpto.getFullYear() + 1)

  const formSchema = getFormSchema(t)
  const form = useForm<TenantFormValues>({
    resolver: zodResolver(formSchema),
    defaultValues: isEdit
      ? {
          id: currentRow.id ?? '',
          name: currentRow.name ?? '',
          identifier: currentRow.identifier ?? '',
          adminEmail: currentRow.adminEmail ?? '',
          adminPassword: '',
          connectionString: currentRow.connectionString ?? '',
          issuer: currentRow.issuer ?? '',
          isActive: currentRow.isActive ?? true,
          validUpto: currentRow.validUpto ? format(new Date(currentRow.validUpto), 'yyyy-MM-dd') : format(defaultValidUpto, 'yyyy-MM-dd'),
        }
      : {
          id: '',
          name: '',
          identifier: '',
          adminEmail: '',
          adminPassword: '',
          connectionString: '',
          issuer: '',
          isActive: true,
          validUpto: format(defaultValidUpto, 'yyyy-MM-dd'),
        },
  })

  const onSubmit = async (values: TenantFormValues) => {
    try {
      if (isEdit) {
        await updateTenant({
          id: values.id,
          data: {
            id: values.id,
            name: values.name,
            connectionString: values.connectionString || null,
            adminEmail: values.adminEmail,
            issuer: values.issuer || null,
            isActive: values.isActive,
            validUpto: new Date(values.validUpto).toISOString()
          }
        })
        toast.success(t('Tenant updated successfully'))
      } else {
        await createTenant({
          data: {
            id: values.id,
            name: values.name,
            adminPassword: null,
            connectionString: values.connectionString || null,
            adminEmail: values.adminEmail,
            issuer: values.issuer || null
          }
        })
        toast.success(t('Tenant created successfully'))
      }
      queryClient.invalidateQueries({ queryKey: ['tenants'] })
      onOpenChange(false)
      form.reset()
    } catch (error: any) {
      toast.error(error?.response?.data?.title || error?.message || t('Operation failed'))
    }
  }

  return (
    <Dialog open={open} onOpenChange={(val) => {
      onOpenChange(val)
      if (!val) form.reset()
    }}>
      <DialogContent className='sm:max-w-md'>
        <DialogHeader className='text-left'>
          <DialogTitle>{isEdit ? t('Edit Tenant') : t('Create Tenant')}</DialogTitle>
          <DialogDescription>
            {isEdit ? t('Update tenant details here.') : t('Create a new tenant here.')}
          </DialogDescription>
        </DialogHeader>
        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className='space-y-4'>
            <FormField
              control={form.control}
              name='id'
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('Tenant ID')} <span className='text-red-500'>*</span></FormLabel>
                  <FormControl>
                    <Input placeholder='tenant1' disabled={isEdit} {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name='name'
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('Name')} <span className='text-red-500'>*</span></FormLabel>
                  <FormControl>
                    <Input placeholder='Default Tenant' {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            {isEdit && (
              <FormField
                control={form.control}
                name='identifier'
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('Identifier')}</FormLabel>
                    <FormControl>
                      <Input disabled {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
            )}
            <FormField
              control={form.control}
              name='adminEmail'
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('Admin Email')} <span className='text-red-500'>*</span></FormLabel>
                  <FormControl>
                    <Input placeholder='admin@tenant.com' type="email" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name='connectionString'
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('Connection String')} <span className="text-muted-foreground text-xs font-normal">({t('Optional')})</span></FormLabel>
                  <FormControl>
                    <Input placeholder='Host=...;Database=...;' {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name='issuer'
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('Issuer')} <span className="text-muted-foreground text-xs font-normal">({t('Optional')})</span></FormLabel>
                  <FormControl>
                    <Input placeholder='https://my-tenant.com' {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            {isEdit && (
              <div className="flex gap-4">
                  <FormField
                  control={form.control}
                  name='isActive'
                  render={({ field }) => (
                    <FormItem className="flex flex-col flex-1 space-y-3 justify-center">
                      <FormLabel>{t('Active Status')}</FormLabel>
                      <FormControl>
                        <Switch
                          checked={field.value}
                          onCheckedChange={field.onChange}
                        />
                      </FormControl>
                    </FormItem>
                  )}
                />
                <FormField
                  control={form.control}
                  name='validUpto'
                  render={({ field }) => (
                    <FormItem className="flex-1">
                      <FormLabel>{t('Valid Upto')}</FormLabel>
                      <FormControl>
                        <Input type="date" {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>
            )}
            <DialogFooter className='gap-2 sm:gap-0 mt-4'>
              <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
                {t('Cancel')}
              </Button>
              <Button type="submit" disabled={isCreating || isUpdating}>
                {isEdit ? t('Save') : t('Create')}
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  )
}
