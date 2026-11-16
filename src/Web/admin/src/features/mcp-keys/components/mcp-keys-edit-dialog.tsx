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
import { useState } from 'react'
import { Input } from '@/components/ui/input'
import { type McpKeyDto as McpKey } from '@/api/model'
import { McpKeyScopeEditor } from './mcp-key-scope-editor'
import { useUpdateMcpKey } from '@/api/endpoints/mcp-keys'
import { fromRow, toPayload, validateScope } from '../lib/key-scope'
import type { McpKeyScopeValue } from '../types'

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
  const [scope, setScope] = useState<McpKeyScopeValue>(() => fromRow(currentRow))
  const [scopeError, setScopeError] = useState<string | null>(null)

  const form = useForm<FormValues>({
    resolver: zodResolver(getFormSchema(t)),
    defaultValues: { name: currentRow?.name ?? '' },
  })

  const onSubmit = (values: FormValues) => {
    if (!currentRow?.id) return
    const invalid = validateScope(scope)
    if (invalid) {
      setScopeError(
        invalid.code === 'no-server'
          ? t('Select at least one server.')
          : t('Select at least one tool.')
      )
      return
    }
    setScopeError(null)

    updateMutation.mutate(
      { id: currentRow.id, data: toPayload(values.name, scope) },
      {
        onSuccess: () => {
          toast.success(t('MCP key updated successfully'))
          form.reset()
          onOpenChange(false)
          queryClient.invalidateQueries({ queryKey: ['mcp-keys'] })
        },
        onError: (err: unknown) =>
          toast.error(
            (err as { response?: { data?: { message?: string } } })?.response?.data?.message ||
              t('Failed to update MCP key.')
          ),
      }
    )
  }

  return (
    <Dialog
      open={open}
      onOpenChange={(state) => {
        form.reset()
        setScope(fromRow(currentRow))
        setScopeError(null)
        onOpenChange(state)
      }}
    >
      <DialogContent className='sm:max-w-2xl [&>*]:min-w-0'>
        <DialogHeader className='text-start'>
          <DialogTitle>{t('Edit MCP Key')}</DialogTitle>
          <DialogDescription>
            {t('Update the key name and access scope. The key value cannot be changed.')}{' '}
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
                <FormItem className='grid grid-cols-1 items-center gap-1 space-y-0 sm:grid-cols-6 sm:gap-x-4'>
                  <FormLabel className='sm:col-span-2 sm:text-end'>{t('Name')}</FormLabel>
                  <FormControl>
                    <Input className='w-full sm:col-span-4' {...field} />
                  </FormControl>
                  <FormMessage className='sm:col-span-4 sm:col-start-3' />
                </FormItem>
              )}
            />
                <div className='space-y-2'>
                  <span className='text-sm font-medium'>{t('Access Scope')}</span>
                  <McpKeyScopeEditor
                    value={scope}
                    onChange={(next) => {
                      setScope(next)
                      setScopeError(null)
                    }}
                  />
                  {scopeError && (
                    <p className='text-xs text-destructive'>{scopeError}</p>
                  )}
                </div>
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
