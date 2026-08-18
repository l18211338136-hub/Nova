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
import { toast } from 'sonner'
import { useQueryClient } from '@tanstack/react-query'
import { DictionaryTypeDto } from '@/api/model'
import {
  useCreateDictionaryType,
  useUpdateDictionaryType,
  getDictionaryTypesQueryKey as getTypesQueryKey,
} from '@/api/endpoints/dictionaries'
import { useTranslation } from 'react-i18next'

const formSchema = z.object({
  code: z.string().min(1, '请输入字典编码').max(64, '编码不超过64字符'),
  name: z.string().min(1, '请输入字典名称').max(64, '名称不超过64字符'),
  description: z.string().optional(),
  isEnabled: z.boolean(),
  sortOrder: z.number(),
})

type FormValues = z.infer<typeof formSchema>

interface DictTypeDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  editingType?: DictionaryTypeDto | null
}

export function DictTypeDialog({ open, onOpenChange, editingType }: DictTypeDialogProps) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const createMutation = useCreateDictionaryType()
  const updateMutation = useUpdateDictionaryType()

  const form = useForm<FormValues>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      code: '',
      name: '',
      description: '',
      isEnabled: true,
      sortOrder: 0,
    },
  })

  useEffect(() => {
    if (editingType) {
      form.reset({
        code: editingType.code || '',
        name: editingType.name || '',
        description: editingType.description || '',
        isEnabled: editingType.isEnabled ?? true,
        sortOrder: editingType.sortOrder ?? 0,
      })
    } else {
      form.reset({
        code: '',
        name: '',
        description: '',
        isEnabled: true,
        sortOrder: 0,
      })
    }
  }, [editingType, open, form])

  const onSubmit = async (values: FormValues) => {
    try {
      if (editingType) {
        await updateMutation.mutateAsync({
          id: editingType.id!,
          data: {
            name: values.name,
            description: values.description,
            isEnabled: values.isEnabled,
            sortOrder: values.sortOrder,
          },
        })
        toast.success(t('字典类型更新成功'))
      } else {
        await createMutation.mutateAsync({
          data: {
            code: values.code,
            name: values.name,
            description: values.description,
            isEnabled: values.isEnabled,
            sortOrder: values.sortOrder,
          },
        })
        toast.success(t('字典类型创建成功'))
      }
      await queryClient.invalidateQueries({ queryKey: getTypesQueryKey() })
      onOpenChange(false)
    } catch (err: any) {
      toast.error(err?.message || '操作失败')
    }
  }

  const isLoading = createMutation.isPending || updateMutation.isPending

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className='sm:max-w-[425px]'>
        <DialogHeader>
          <DialogTitle>{editingType ? t('编辑字典类型') : t('新增字典类型')}</DialogTitle>
          <DialogDescription>
            {editingType ? t('修改字典分类的描述及状态') : t('添加一个新的数据字典分类类型')}
          </DialogDescription>
        </DialogHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className='space-y-4'>
            <FormField
              control={form.control}
              name='code'
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('字典编码')}</FormLabel>
                  <FormControl>
                    <Input
                      placeholder='如: sys_user_gender'
                      disabled={!!editingType}
                      {...field}
                    />
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
                  <FormLabel>{t('字典名称')}</FormLabel>
                  <FormControl>
                    <Input placeholder='如: 用户性别' {...field} />
                  </FormControl>
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

            <FormField
              control={form.control}
              name='description'
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('描述备注')}</FormLabel>
                  <FormControl>
                    <Textarea placeholder='请输入描述信息' {...field} />
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
