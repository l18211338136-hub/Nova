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
import { useCreateMcpKey } from '@/api/endpoints/mcp-keys'

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
  const createMutation = useCreateMcpKey()

  const form = useForm<FormValues>({
    resolver: zodResolver(getFormSchema(t)),
    defaultValues: { name: '' },
  })

  const handleOpenChange = (state: boolean) => {
    if (!state) {
      setGeneratedKey(null)
      setCopied(false)
      form.reset()
    }
    onOpenChange(state)
  }

  const onSubmit = (values: FormValues) => {
    createMutation.mutate(
      { data: { name: values.name } },
      {
        onSuccess: (res) => {
          const key = res?.data?.keyValue
          if (key) setGeneratedKey(key)
          queryClient.invalidateQueries({ queryKey: ['mcp-keys'] })
          toast.success(t('MCP key created successfully'))
          form.reset()
        },
        onError: (err: { response?: { data?: { message?: string } } }) =>
          toast.error(err?.response?.data?.message || t('Failed to create MCP key.')),
      }
    )
  }

  const handleCopy = async () => {
    if (!generatedKey) return
    try {
      await navigator.clipboard.writeText(generatedKey)
      setCopied(true)
      toast.success(t('Copied'))
      window.setTimeout(() => setCopied(false), 2000)
    } catch {
      toast.error(t('Copy failed, please copy manually'))
    }
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className='sm:max-w-lg'>
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
                    <FormItem className='grid grid-cols-6 items-center space-y-0 gap-x-4 gap-y-1'>
                      <FormLabel className='col-span-2 text-end'>
                        {t('Name')}
                      </FormLabel>
                      <FormControl>
                        <Input
                          placeholder='my-agent-key'
                          className='col-span-4'
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
