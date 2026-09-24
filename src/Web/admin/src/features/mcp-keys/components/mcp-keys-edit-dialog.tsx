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
import { type McpKeyDto as McpKey } from '@/api/model'
import { useUpdateMcpKey } from '@/api/endpoints/mcp-keys'

const getFormSchema = (t: (arg: string) => string) =>
  z.object({
    name: z.string().min(1, t('Name is required.')),
  })

type FormValues = z.infer<ReturnType<typeof getFormSchema>>

type McpKeysEditDialogProps = {
  currentRow?: McpKey
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function McpKeysEditDialog({
  currentRow,
  open,
  onOpenChange,
}: McpKeysEditDialogProps) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const updateMutation = useUpdateMcpKey()

  const form = useForm<FormValues>({
    resolver: zodResolver(getFormSchema(t)),
    defaultValues: { name: currentRow?.name ?? '' },
  })

  const onSubmit = (values: FormValues) => {
    if (!currentRow?.id) return
    updateMutation.mutate(
      { id: currentRow.id, data: { name: values.name } },
      {
        onSuccess: () => {
          toast.success(t('MCP key updated successfully'))
          form.reset()
          onOpenChange(false)
          queryClient.invalidateQueries({ queryKey: ['mcp-keys'] })
        },
        onError: (err: { response?: { data?: { message?: string } } }) =>
          toast.error(err?.response?.data?.message || t('Failed to update MCP key.')),
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
          <DialogTitle>{t('Edit MCP Key')}</DialogTitle>
          <DialogDescription>
            {t('Update the key name. The key value cannot be changed.')}{' '}
            {t("Click save when you're done.")}
          </DialogDescription>
        </DialogHeader>
        <Form {...form}>
          <form
            id='mcp-key-edit-form'
            onSubmit={form.handleSubmit(onSubmit)}
            className='space-y-4 px-0.5 py-1'
          >
            <FormField
              control={form.control}
              name='name'
              render={({ field }) => (
                <FormItem className='grid grid-cols-6 items-center space-y-0 gap-x-4 gap-y-1'>
                  <FormLabel className='col-span-2 text-end'>{t('Name')}</FormLabel>
                  <FormControl>
                    <Input className='col-span-4' {...field} />
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
            form='mcp-key-edit-form'
            disabled={updateMutation.isPending}
          >
            {t('Save')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
