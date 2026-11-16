'use client'

import { z } from 'zod'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { zodResolver } from '@hookform/resolvers/zod'
import { useQueryClient } from '@tanstack/react-query'
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
import { useUpdateMcpServer } from '@/api/endpoints/mcp'
import type { McpServerDto } from '../types'

const getFormSchema = (t: (arg: string) => string) =>
  z.object({
    name: z.string().trim().min(1, t('Server Name is required.')),
    baseUrl: z
      .string()
      .trim()
      .min(1, t('Base URL is required.'))
      .url(t('Please enter a valid URL.')),
    // 可选：留空时提交 null，由后端沿用原值
    swaggerUrl: z
      .union([z.literal(''), z.string().trim().url(t('Please enter a valid URL.'))])
      .optional(),
    authToken: z.string().optional(),
  })

type FormValues = z.infer<ReturnType<typeof getFormSchema>>

type McpEditDialogProps = {
  currentRow?: McpServerDto
  open: boolean
  onOpenChange: (open: boolean) => void
}

/**
 * 编辑 MCP 服务基本信息。
 *
 * 范式照搬 `mcp-keys-edit-dialog.tsx`：
 * - 由 `mcp-dialogs.tsx` 传 `key` 强制重挂载，保证切换行时 defaultValues 刷新；
 * - 提交成功后 invalidate `['mcp-servers']` 并关闭。
 *
 * 🔺 `authToken` 不回填、留空不提交：后端一次编辑若以 null 覆盖，会清掉已存的
 * 认证令牌（见《01-需求与范围》§5）。
 */
export function McpEditDialog({
  currentRow,
  open,
  onOpenChange,
}: McpEditDialogProps) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const updateMutation = useUpdateMcpServer()

  const form = useForm<FormValues>({
    resolver: zodResolver(getFormSchema(t)),
    defaultValues: {
      name: currentRow?.name ?? '',
      baseUrl: currentRow?.baseUrl ?? '',
      swaggerUrl: currentRow?.swaggerUrl ?? '',
      authToken: '',
    },
  })

  const onSubmit = (values: FormValues) => {
    if (!currentRow?.id) return
    updateMutation.mutate(
      {
        id: currentRow.id,
        data: {
          name: values.name,
          baseUrl: values.baseUrl,
          swaggerUrl: values.swaggerUrl?.trim() ? values.swaggerUrl.trim() : null,
          // 留空 → 不传（而非传 null），避免覆盖已有令牌
          ...(values.authToken?.trim() ? { authToken: values.authToken.trim() } : {}),
        },
      },
      {
        onSuccess: () => {
          toast.success(t('MCP server updated successfully'))
          onOpenChange(false)
          queryClient.invalidateQueries({ queryKey: ['mcp-servers'] })
        },
        onError: (err: unknown) =>
          toast.error(
            (err as { response?: { data?: { message?: string } } })?.response
              ?.data?.message || t('Failed to update MCP server.')
          ),
      }
    )
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
        <DialogHeader className='text-start'>
          <DialogTitle>{t('Edit MCP Server')}</DialogTitle>
          <DialogDescription>
            {t('Update the basic information of this MCP server.')}{' '}
            {t("Click save when you're done.")}
          </DialogDescription>
        </DialogHeader>
        <Form {...form}>
          <form
            id='mcp-server-edit-form'
            onSubmit={form.handleSubmit(onSubmit)}
            className='space-y-4 px-0.5 py-1'
          >
            <FormField
              control={form.control}
              name='name'
              render={({ field }) => (
                <FormItem className='grid grid-cols-6 items-center space-y-0 gap-x-4 gap-y-1'>
                  <FormLabel className='col-span-2 text-end'>
                    {t('Server Name')}
                  </FormLabel>
                  <FormControl>
                    <Input className='col-span-4' {...field} />
                  </FormControl>
                  <FormMessage className='col-span-4 col-start-3' />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name='baseUrl'
              render={({ field }) => (
                <FormItem className='grid grid-cols-6 items-center space-y-0 gap-x-4 gap-y-1'>
                  <FormLabel className='col-span-2 text-end'>
                    {t('Base URL')}
                  </FormLabel>
                  <FormControl>
                    <Input className='col-span-4' {...field} />
                  </FormControl>
                  <FormMessage className='col-span-4 col-start-3' />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name='swaggerUrl'
              render={({ field }) => (
                <FormItem className='grid grid-cols-6 items-center space-y-0 gap-x-4 gap-y-1'>
                  <FormLabel className='col-span-2 text-end'>
                    {t('Swagger URL')}
                  </FormLabel>
                  <FormControl>
                    <Input
                      className='col-span-4'
                      placeholder={t('Swagger URL is optional.')}
                      {...field}
                    />
                  </FormControl>
                  <FormMessage className='col-span-4 col-start-3' />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name='authToken'
              render={({ field }) => (
                <FormItem className='grid grid-cols-6 items-center space-y-0 gap-x-4 gap-y-1'>
                  <FormLabel className='col-span-2 text-end'>
                    {t('Auth Token')}
                  </FormLabel>
                  <FormControl>
                    <Input
                      className='col-span-4'
                      type='password'
                      autoComplete='new-password'
                      placeholder={t('Leave blank to keep the current token.')}
                      {...field}
                    />
                  </FormControl>
                  <FormMessage className='col-span-4 col-start-3' />
                </FormItem>
              )}
            />
          </form>
        </Form>
        <DialogFooter>
          <Button
            type='submit'
            form='mcp-server-edit-form'
            disabled={updateMutation.isPending}
          >
            {updateMutation.isPending ? t('Saving...') : t('Save')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
