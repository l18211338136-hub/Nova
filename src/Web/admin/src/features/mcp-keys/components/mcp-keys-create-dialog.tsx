'use client'

import { useState } from 'react'
import { z } from 'zod'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { zodResolver } from '@hookform/resolvers/zod'
import { useQueryClient } from '@tanstack/react-query'
import { Check, Copy } from 'lucide-react'
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
import { copyToClipboard } from '@/lib/clipboard'
import { McpKeyScopeEditor } from './mcp-key-scope-editor'
import { useCreateMcpKeyWithScope } from '../hooks/use-mcp-key-mutations'
import { toPayload, validateScope } from '../lib/key-scope'
import { DEFAULT_SCOPE, type McpKeyScopeValue } from '../types'

const getFormSchema = (t: (arg: string) => string) =>
  z.object({
    name: z.string().min(1, t('Name is required.')),
  })

type FormValues = z.infer<ReturnType<typeof getFormSchema>>

type McpKeysCreateDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function McpKeysCreateDialog({
  open,
  onOpenChange,
}: McpKeysCreateDialogProps) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const [generatedKey, setGeneratedKey] = useState<string | null>(null)
  const [copied, setCopied] = useState(false)
  const [scope, setScope] = useState<McpKeyScopeValue>(DEFAULT_SCOPE)
  const [scopeError, setScopeError] = useState<string | null>(null)
  const createMutation = useCreateMcpKeyWithScope()

  const form = useForm<FormValues>({
    resolver: zodResolver(getFormSchema(t)),
    defaultValues: { name: '' },
  })

  const handleOpenChange = (state: boolean) => {
    if (!state) {
      setGeneratedKey(null)
      setCopied(false)
      setScope(DEFAULT_SCOPE)
      setScopeError(null)
      form.reset()
    }
    onOpenChange(state)
  }

  const onSubmit = (values: FormValues) => {
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

    createMutation.mutate(toPayload(values.name, scope), {
      onSuccess: (res) => {
        const key = res?.data?.keyValue
        if (key) setGeneratedKey(key)
        queryClient.invalidateQueries({ queryKey: ['mcp-keys'] })
        toast.success(t('MCP key created successfully'))
        form.reset()
      },
      onError: (err: unknown) =>
        toast.error(
          (err as { response?: { data?: { message?: string } } })?.response?.data?.message ||
            t('Failed to create MCP key.')
        ),
    })
  }

  const handleCopy = async () => {
    if (!generatedKey) return
    try {
      await copyToClipboard(generatedKey)
      setCopied(true)
      toast.success(t('Copied'))
      window.setTimeout(() => setCopied(false), 2000)
    } catch {
      toast.error(t('Copy failed, please copy manually'))
    }
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className='sm:max-w-2xl [&>*]:min-w-0'>
        {generatedKey ? (
          <>
            <DialogHeader>
              <DialogTitle>{t('MCP Key Created')}</DialogTitle>
              <DialogDescription>
                {t('Please copy and store the key now. It will not be shown again.')}
              </DialogDescription>
            </DialogHeader>
            <div className='rounded-md border bg-muted/50 p-3'>
              <code
                className='block break-all font-mono text-xs notranslate'
                translate='no'
              >
                {generatedKey}
              </code>
            </div>
            <DialogFooter>
              <Button type='button' variant='outline' onClick={handleCopy}>
                {copied ? <Check size={15} className='me-1' /> : <Copy size={15} className='me-1' />}
                {t('Copy Key')}
              </Button>
              <Button type='button' onClick={() => handleOpenChange(false)}>
                {t('Done')}
              </Button>
            </DialogFooter>
          </>
        ) : (
          <>
            <DialogHeader className='text-start'>
              <DialogTitle>{t('New MCP Key')}</DialogTitle>
              <DialogDescription>
                {t('Create a new MCP access key.')}{' '}
                {t("Click save when you're done.")}
              </DialogDescription>
            </DialogHeader>
            <Form {...form}>
              <form
                id='mcp-key-create-form'
                onSubmit={form.handleSubmit(onSubmit)}
                className='space-y-4 px-0.5 py-1'
              >
                <FormField
                  control={form.control}
                  name='name'
                  render={({ field }) => (
                    <FormItem className='grid grid-cols-1 items-center gap-1 space-y-0 sm:grid-cols-6 sm:gap-x-4'>
                      <FormLabel className='sm:col-span-2 sm:text-end'>
                        {t('Name')}
                      </FormLabel>
                      <FormControl>
                        <Input
                          placeholder='my-agent-key'
                          className='w-full sm:col-span-4'
                          {...field}
                        />
                      </FormControl>
                      <FormMessage className='sm:col-span-4 sm:col-start-3' />
                    </FormItem>
                  )}
                />
                <div className='space-y-2'>
                  <div className='flex items-center justify-between'>
                    <span className='text-sm font-medium'>{t('Access Scope')}</span>
                    <span className='text-xs text-muted-foreground'>
                      {t('This key can only call the selected servers and tools.')}
                    </span>
                  </div>
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
                form='mcp-key-create-form'
                disabled={createMutation.isPending}
              >
                {t('Save')}
              </Button>
            </DialogFooter>
          </>
        )}
      </DialogContent>
    </Dialog>
  )
}
