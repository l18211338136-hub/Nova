import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
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
import { Button } from '@/components/ui/button'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { toast } from 'sonner'
import { useQueryClient } from '@tanstack/react-query'
import { DictionaryItemDto } from '@/api/model'
import {
  useCreateDictionaryItem,
  useUpdateDictionaryItem,
  getDictionaryItemsQueryKey as getItemsQueryKey,
} from '@/api/endpoints/dictionaries'
import { useTranslation } from 'react-i18next'

const formSchema = z.object({
  label: z.string().min(1, '请输入字典显示文本').max(64, '显示文本不超过64字符'),
  value: z.string().min(1, '请输入字典数据值').max(128, '数据值不超过128字符'),
  tagType: z.string(),
  sortOrder: z.number(),
  isDefault: z.boolean(),
  isEnabled: z.boolean(),
  remarks: z.string().optional(),
})

type FormValues = z.infer<typeof formSchema>

interface DictItemDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  typeId: string
  editingItem?: DictionaryItemDto | null
}

export function DictItemDialog({ open, onOpenChange, typeId, editingItem }: DictItemDialogProps) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const createMutation = useCreateDictionaryItem()
  const updateMutation = useUpdateDictionaryItem()

  const form = useForm<FormValues>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      label: '',
      value: '',
      tagType: 'default',
      sortOrder: 0,
      isDefault: false,
      isEnabled: true,
      remarks: '',
    },
  })

  useEffect(() => {
    if (editingItem) {
      form.reset({
        label: editingItem.label || '',
        value: editingItem.value || '',
        tagType: editingItem.tagType || 'default',
        sortOrder: editingItem.sortOrder ?? 0,
        isDefault: editingItem.isDefault ?? false,
        isEnabled: editingItem.isEnabled ?? true,
        remarks: editingItem.remarks || '',
      })
    } else {
      form.reset({
        label: '',
        value: '',
        tagType: 'default',
        sortOrder: 0,
        isDefault: false,
        isEnabled: true,
        remarks: '',
      })
    }
  }, [editingItem, open, form])

  const onSubmit = async (values: FormValues) => {
    try {
      if (editingItem) {
        await updateMutation.mutateAsync({
          id: editingItem.id!,
          data: {
            label: values.label,
            value: values.value,
            tagType: values.tagType,
            sortOrder: values.sortOrder,
            isDefault: values.isDefault,
            isEnabled: values.isEnabled,
            remarks: values.remarks,
          },
        })
        toast.success(t('字典数据项更新成功'))
      } else {
        await createMutation.mutateAsync({
          data: {
            typeId,
            label: values.label,
            value: values.value,
            tagType: values.tagType,
            sortOrder: values.sortOrder,
            isDefault: values.isDefault,
            isEnabled: values.isEnabled,
            remarks: values.remarks,
          },
        })
        toast.success(t('字典数据项创建成功'))
      }
      await queryClient.invalidateQueries({ queryKey: getItemsQueryKey() })
      onOpenChange(false)
    } catch (err: any) {
      toast.error(err?.message || '操作失败')
    }
  }

  const isLoading = createMutation.isPending || updateMutation.isPending

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className='sm:max-w-[450px]'>
        <DialogHeader>
          <DialogTitle>{editingItem ? t('编辑字典数据项') : t('新增字典数据项')}</DialogTitle>
          <DialogDescription>
            {editingItem ? t('修改字典数据项的详细信息') : t('添加一个新的字典数据项')}
          </DialogDescription>
        </DialogHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className='space-y-4'>
            <div className='grid grid-cols-2 gap-4'>
              <FormField
                control={form.control}
                name='label'
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('显示文本')}</FormLabel>
                    <FormControl>
                      <Input placeholder='如: 男' {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name='value'
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('数据值')}</FormLabel>
                    <FormControl>
                      <Input placeholder='如: M 或 1' {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </div>

            <div className='grid grid-cols-2 gap-4'>
              <FormField
                control={form.control}
                name='tagType'
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('标签类型')}</FormLabel>
                    <Select onValueChange={field.onChange} defaultValue={field.value}>
                      <FormControl>
                        <SelectTrigger className='w-full'>
                          <SelectValue placeholder='选择类型' />
                        </SelectTrigger>
                      </FormControl>
                      <SelectContent>
                        <SelectItem value='default'>{t('默认')}</SelectItem>
                        <SelectItem value='primary'>{t('主要')}</SelectItem>
                        <SelectItem value='success'>{t('成功')}</SelectItem>
                        <SelectItem value='warning'>{t('警告')}</SelectItem>
                        <SelectItem value='danger'>{t('危险')}</SelectItem>
                        <SelectItem value='info'>{t('信息')}</SelectItem>
                      </SelectContent>
                    </Select>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name='sortOrder'
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('排序号')}</FormLabel>
                    <FormControl>
                      <Input
                        type='number'
                        placeholder='0'
                        {...field}
                        onChange={(e) => field.onChange(Number(e.target.value))}
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </div>

            <div className='grid grid-cols-2 gap-4 py-1'>
              <FormField
                control={form.control}
                name='isDefault'
                render={({ field }) => (
                  <FormItem className='flex items-center justify-between rounded-md border px-3 py-2'>
                    <FormLabel className='text-sm font-normal cursor-pointer'>{t('默认选中')}</FormLabel>
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
                name='isEnabled'
                render={({ field }) => (
                  <FormItem className='flex items-center justify-between rounded-md border px-3 py-2'>
                    <FormLabel className='text-sm font-normal cursor-pointer'>{t('是否启用')}</FormLabel>
                    <FormControl>
                      <Switch
                        checked={field.value}
                        onCheckedChange={field.onChange}
                      />
                    </FormControl>
                  </FormItem>
                )}
              />
            </div>

            <FormField
              control={form.control}
              name='remarks'
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('备注信息')}</FormLabel>
                  <FormControl>
                    <Textarea placeholder='请输入备注' {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <DialogFooter>
              <Button type='button' variant='outline' onClick={() => onOpenChange(false)}>
                {t('取消')}
              </Button>
              <Button type='submit' disabled={isLoading}>
                {isLoading ? t('保存中...') : t('保存')}
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  )
}
