'use client'

import { useMemo, useRef, useState } from 'react'
import { z } from 'zod'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { zodResolver } from '@hookform/resolvers/zod'
import { useQueryClient } from '@tanstack/react-query'
import { ChevronDown, Upload } from 'lucide-react'
import { useImport } from '@/api/endpoints/mcp'
import { type ImportOpenApi } from '@/api/model'
import { toast } from 'sonner'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { Checkbox } from '@/components/ui/checkbox'
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
import {
  type ImportOpenApiPayload,
  type ParsedSwaggerOperation,
} from '../types'
import { useMcpContext } from './mcp-provider'
import { useParseSwagger } from '../hooks/use-parse-swagger'

const getFormSchema = (t: (key: string) => string) =>
  z
    .object({
      serverName: z.string().min(1, t('Server Name is required.')),
      baseUrl: z
        .string()
        .min(1, t('Base URL is required.'))
        .url(t('Please enter a valid URL.')),
      swaggerUrl: z.string().optional(),
      swaggerJson: z.string().optional(),
      authToken: z.string().optional(),
    })
    .superRefine((values, ctx) => {
      // Swagger 地址 / Swagger JSON 二选一：任填其一即可解析
      const hasUrl = !!values.swaggerUrl?.trim()
      const hasJson = !!values.swaggerJson?.trim()
      if (!hasUrl && !hasJson) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          path: ['swaggerUrl'],
          message: t('Fill in either Swagger URL or Swagger JSON.'),
        })
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          path: ['swaggerJson'],
          message: t('Fill in either Swagger URL or Swagger JSON.'),
        })
        return
      }
      if (
        hasUrl &&
        !z.string().url().safeParse(values.swaggerUrl!.trim()).success
      ) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          path: ['swaggerUrl'],
          message: t('Please enter a valid URL.'),
        })
      }
    })

type ImportForm = z.infer<ReturnType<typeof getFormSchema>>

interface OperationGroup {
  name: string
  ops: ParsedSwaggerOperation[]
}

const METHOD_STYLES: Record<string, string> = {
  GET: 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400',
  POST: 'bg-sky-500/10 text-sky-600 dark:text-sky-400',
  PUT: 'bg-amber-500/10 text-amber-600 dark:text-amber-400',
  DELETE: 'bg-red-500/10 text-red-600 dark:text-red-400',
  PATCH: 'bg-violet-500/10 text-violet-600 dark:text-violet-400',
}

export function McpImportDialog() {
  const { open, setOpen } = useMcpContext()
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const importMutation = useImport()
  const parseMutation = useParseSwagger()
  const fileRef = useRef<HTMLInputElement>(null)

  const [step, setStep] = useState<1 | 2>(1)
  const [operations, setOperations] = useState<ParsedSwaggerOperation[]>([])
  const [selected, setSelected] = useState<Set<string>>(new Set())
  const [search, setSearch] = useState('')
  const [collapsed, setCollapsed] = useState<Set<string>>(new Set())

  const formSchema = getFormSchema(t)
  const form = useForm<ImportForm>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      serverName: '',
      baseUrl: '',
      swaggerUrl: '',
      swaggerJson: '',
      authToken: '',
    },
  })

  // 按分组聚合
  const groups = useMemo<OperationGroup[]>(() => {
    const map = new Map<string, ParsedSwaggerOperation[]>()
    for (const op of operations) {
      const arr = map.get(op.group)
      if (arr) arr.push(op)
      else map.set(op.group, [op])
    }
    return [...map.entries()]
      .map(([name, ops]) => ({ name, ops }))
      .sort((a, b) => a.name.localeCompare(b.name))
  }, [operations])

  // 搜索时按分组过滤（保留有命中的组）
  const filteredGroups = useMemo<OperationGroup[]>(() => {
    const q = search.trim().toLowerCase()
    if (!q) return groups
    const match = (op: ParsedSwaggerOperation) =>
      op.path.toLowerCase().includes(q) ||
      op.summary.toLowerCase().includes(q) ||
      op.method.toLowerCase().includes(q)
    return groups
      .map((g) => ({ ...g, ops: g.ops.filter(match) }))
      .filter((g) => g.ops.length > 0)
  }, [groups, search])

  const isSearching = search.trim().length > 0

  const resetToFirstStep = () => {
    setStep(1)
    setOperations([])
    setSelected(new Set())
    setSearch('')
    setCollapsed(new Set())
  }

  const onFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return
    const text = await file.text()
    form.setValue('swaggerJson', text, { shouldValidate: true })
    e.target.value = ''
  }

  const handleParse = () => {
    form.trigger().then((valid) => {
      if (!valid) return
      const values = form.getValues()
      parseMutation.mutate(
        {
          swaggerUrl: values.swaggerUrl?.trim() || null,
          swaggerJson: values.swaggerJson?.trim() || null,
          authToken: values.authToken?.trim() || null,
        },
        {
          onSuccess: (data) => {
            const operations = data?.operations ?? []
            if (operations.length === 0) {
              toast.error(t('No endpoints found in the document.'))
              return
            }
            const ops: ParsedSwaggerOperation[] = operations.map((op) => ({
              key: `${op.method.toUpperCase()} ${op.path}`,
              method: op.method.toUpperCase(),
              path: op.path,
              summary: op.summary ?? '',
              group: op.group || '/',
            }))
            // 把最终用于解析的 Swagger JSON 回填到最下方文本框，
            // 无论来源是 Swagger 地址还是手动粘贴/上传，此处保持一致
            form.setValue('swaggerJson', data?.swaggerJson ?? '')
            setOperations(ops)
            setSelected(new Set(ops.map((op) => op.key)))
            setSearch('')
            setCollapsed(new Set())
            setStep(2)
          },
          onError: (err) => {
            const axiosErr = err as {
              response?: { data?: { message?: string } }
              message?: string
            }
            toast.error(
              axiosErr?.response?.data?.message ??
                t('Failed to parse Swagger.')
            )
          },
        }
      )
    })
  }

  const toggleOperation = (key: string, checked: boolean) => {
    setSelected((prev) => {
      const next = new Set(prev)
      if (checked) next.add(key)
      else next.delete(key)
      return next
    })
  }

  const toggleGroup = (group: OperationGroup, checked: boolean) => {
    setSelected((prev) => {
      const next = new Set(prev)
      for (const op of group.ops) {
        if (checked) next.add(op.key)
        else next.delete(op.key)
      }
      return next
    })
  }

  const selectAllFiltered = (checked: boolean) => {
    setSelected((prev) => {
      const next = new Set(prev)
      for (const g of filteredGroups) {
        for (const op of g.ops) {
          if (checked) next.add(op.key)
          else next.delete(op.key)
        }
      }
      return next
    })
  }

  const toggleExpanded = (name: string) => {
    setCollapsed((prev) => {
      const next = new Set(prev)
      if (next.has(name)) next.delete(name)
      else next.add(name)
      return next
    })
  }

  const onSubmit = (values: ImportForm) => {
    if (selected.size === 0) {
      toast.error(t('Please select at least one endpoint.'))
      return
    }
    const payload: ImportOpenApiPayload = {
      serverName: values.serverName,
      baseUrl: values.baseUrl,
      swaggerUrl: values.swaggerUrl || null,
      swaggerJson: values.swaggerJson ?? '',
      authToken: values.authToken || null,
      selectedOperations: [...selected],
    }
    importMutation.mutate(
      { data: payload as unknown as ImportOpenApi },
      {
        onSuccess: () => {
          toast.success(t('MCP server imported successfully'))
          form.reset()
          resetToFirstStep()
          setOpen(null)
          queryClient.invalidateQueries({ queryKey: ['mcp-servers'] })
          queryClient.invalidateQueries({ queryKey: ['mcp-tools'] })
        },
        onError: (err) => {
          const message = (err as { message?: string } | undefined)?.message
          toast.error(message ?? t('Import failed.'))
        },
      }
    )
  }

  const handleClose = (state: boolean) => {
    form.reset()
    resetToFirstStep()
    setOpen(state ? 'import' : null)
  }

  return (
    <Dialog open={open === 'import'} onOpenChange={handleClose}>
      <DialogContent
        className={cn(
          'flex w-full flex-col overflow-hidden sm:max-w-2xl',
          step === 1 ? 'max-h-[85vh]' : 'h-[85vh]'
        )}
      >
        <DialogHeader className='text-start'>
          <DialogTitle>{t('Import MCP Server')}</DialogTitle>
          <DialogDescription>
            {step === 1
              ? t(
                  'Create an MCP server from an OpenAPI/Swagger specification.'
                )
              : t('Select endpoints to import')}
          </DialogDescription>
        </DialogHeader>

        {step === 1 ? (
          <>
            <div className='min-h-0 flex-1 overflow-y-auto pe-1'>
              <Form {...form}>
                <form
                  id='mcp-import-form'
                  onSubmit={form.handleSubmit(onSubmit)}
                  className='space-y-4 px-0.5'
                >
                  <FormField
                    control={form.control}
                    name='serverName'
                    render={({ field }) => (
                      <FormItem className='space-y-1.5'>
                        <FormLabel>{t('Server Name')}</FormLabel>
                        <FormControl>
                          <Input placeholder='My API Server' {...field} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  <FormField
                    control={form.control}
                    name='baseUrl'
                    render={({ field }) => (
                      <FormItem className='space-y-1.5'>
                        <FormLabel>{t('Base URL')}</FormLabel>
                        <FormControl>
                          <Input
                            placeholder='https://api.example.com'
                            {...field}
                          />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  <div className='grid gap-4 sm:grid-cols-2'>
                    <FormField
                      control={form.control}
                      name='swaggerUrl'
                      render={({ field }) => (
                        <FormItem className='space-y-1.5'>
                          <FormLabel>{t('Swagger URL')}</FormLabel>
                          <FormControl>
                            <Input
                              placeholder='https://api.example.com/swagger/v1/swagger.json'
                              {...field}
                            />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                    <FormField
                      control={form.control}
                      name='authToken'
                      render={({ field }) => (
                        <FormItem className='space-y-1.5'>
                          <FormLabel>{t('Auth Token')}</FormLabel>
                          <FormControl>
                            <Input
                              placeholder='optional'
                              autoComplete='off'
                              {...field}
                            />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                  </div>
                  <p className='text-xs text-muted-foreground'>
                    {t('Fill in either Swagger URL or Swagger JSON.')}
                  </p>
                  <FormField
                    control={form.control}
                    name='swaggerJson'
                    render={({ field }) => (
                      <FormItem className='space-y-1.5'>
                        <FormLabel>{t('Swagger JSON')}</FormLabel>
                        <FormControl>
                          <div className='space-y-2'>
                            <button
                              type='button'
                              onClick={() => fileRef.current?.click()}
                              className='flex w-full flex-col items-center justify-center gap-1 rounded-md border border-dashed py-4 transition-colors hover:border-primary/40 hover:bg-muted/50'
                            >
                              <Upload className='size-4 text-muted-foreground' />
                              <span className='text-sm font-medium'>
                                {t('Upload Swagger JSON')}
                              </span>
                              <span className='text-xs text-muted-foreground'>
                                {t('or paste below')}
                              </span>
                            </button>
                            <input
                              ref={fileRef}
                              type='file'
                              accept='.json,application/json'
                              className='hidden'
                              onChange={onFileChange}
                            />
                            <Textarea
                              placeholder='{ ... OpenAPI / Swagger JSON ... }'
                              className='field-sizing-fixed h-32 resize-none overflow-y-auto font-mono text-xs'
                              {...field}
                            />
                          </div>
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                </form>
              </Form>
            </div>
            <DialogFooter className='border-t pt-3'>
              <Button
                type='button'
                onClick={handleParse}
                disabled={
                  parseMutation.isPending ||
                  (!form.watch('swaggerUrl')?.trim() &&
                    !form.watch('swaggerJson')?.trim())
                }
              >
                {parseMutation.isPending
                  ? t('Parsing...')
                  : t('Parse endpoints')}
              </Button>
            </DialogFooter>
          </>
        ) : (
          <>
            <div className='flex min-h-0 flex-1 flex-col gap-2 py-0.5'>
              <div className='flex items-center gap-2'>
                <Input
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                  placeholder={t('Search endpoints...')}
                  className='h-8 flex-1'
                />
                <Button
                  type='button'
                  variant='outline'
                  size='sm'
                  onClick={() => selectAllFiltered(true)}
                >
                  {t('Select all')}
                </Button>
                <Button
                  type='button'
                  variant='outline'
                  size='sm'
                  onClick={() => selectAllFiltered(false)}
                >
                  {t('Clear')}
                </Button>
              </div>
              <div className='flex items-center justify-between'>
                <span className='text-xs text-muted-foreground'>
                  {t('Selected {{selected}} / {{total}} endpoints', {
                    selected: selected.size,
                    total: operations.length,
                  })}
                </span>
                <div className='flex items-center gap-1'>
                  <Button
                    type='button'
                    variant='ghost'
                    size='sm'
                    className='h-7 px-2 text-xs'
                    onClick={() => setCollapsed(new Set())}
                  >
                    {t('Expand all')}
                  </Button>
                  <Button
                    type='button'
                    variant='ghost'
                    size='sm'
                    className='h-7 px-2 text-xs'
                    onClick={() =>
                      setCollapsed(new Set(groups.map((g) => g.name)))
                    }
                  >
                    {t('Collapse all')}
                  </Button>
                </div>
              </div>
              <div className='min-h-0 flex-1 overflow-y-auto overscroll-contain rounded-md border'>
                {filteredGroups.length === 0 ? (
                  <div className='flex h-full items-center justify-center text-sm text-muted-foreground'>
                    {t('No results found.')}
                  </div>
                ) : (
                  filteredGroups.map((group) => {
                    const expanded = isSearching || !collapsed.has(group.name)
                    const selectedCount = group.ops.filter((op) =>
                      selected.has(op.key)
                    ).length
                    const allChecked = selectedCount === group.ops.length
                    return (
                      <div key={group.name}>
                        {/* 组头：粘性 + 三态复选 + 折叠（纯色背景，backdrop-blur 会在 transform 容器内触发 Chrome 合成层错位 bug） */}
                        <div className='sticky top-0 z-10 flex items-center gap-2 border-b bg-muted px-2 py-1.5 first:rounded-t-md'>
                          <Checkbox
                            checked={
                              allChecked
                                ? true
                                : selectedCount > 0
                                  ? 'indeterminate'
                                  : false
                            }
                            onCheckedChange={(v) =>
                              toggleGroup(group, v === true)
                            }
                          />
                          <button
                            type='button'
                            onClick={() => toggleExpanded(group.name)}
                            className='flex min-w-0 flex-1 items-center gap-1.5 text-start'
                          >
                            <ChevronDown
                              className={cn(
                                'size-3.5 shrink-0 text-muted-foreground transition-transform',
                                !expanded && '-rotate-90'
                              )}
                            />
                            <span className='truncate text-xs font-medium'>
                              {group.name}
                            </span>
                          </button>
                          <span className='shrink-0 rounded-full bg-background px-1.5 py-0.5 text-[10px] tabular-nums text-muted-foreground'>
                            {selectedCount}/{group.ops.length}
                          </span>
                        </div>
                        {expanded && (
                          <div className='pb-1'>
                            {group.ops.map((op) => (
                              <label
                                key={op.key}
                                className='flex cursor-pointer items-center gap-2 rounded px-2 py-1 ps-8 hover:bg-muted/50'
                              >
                                <Checkbox
                                  checked={selected.has(op.key)}
                                  onCheckedChange={(v) =>
                                    toggleOperation(op.key, v === true)
                                  }
                                />
                                <span
                                  className={cn(
                                    'inline-block w-12 shrink-0 rounded px-1 py-0.5 text-center font-mono text-[10px] font-medium leading-4',
                                    METHOD_STYLES[op.method] ??
                                      'bg-muted text-muted-foreground'
                                  )}
                                >
                                  {op.method}
                                </span>
                                <span className='min-w-0 flex-1 truncate font-mono text-xs'>
                                  {op.path}
                                </span>
                                {op.summary && (
                                  <span className='hidden w-40 shrink-0 truncate text-xs text-muted-foreground sm:block'>
                                    {op.summary}
                                  </span>
                                )}
                              </label>
                            ))}
                          </div>
                        )}
                      </div>
                    )
                  })
                )}
              </div>
            </div>
            <DialogFooter className='border-t pt-3'>
              <Button
                type='button'
                variant='outline'
                onClick={() => setStep(1)}
                disabled={importMutation.isPending}
              >
                {t('Back')}
              </Button>
              <Button
                type='button'
                onClick={form.handleSubmit(onSubmit)}
                disabled={importMutation.isPending || selected.size === 0}
              >
                {importMutation.isPending
                  ? t('Importing...')
                  : t('Import selected ({{count}})', { count: selected.size })}
              </Button>
            </DialogFooter>
          </>
        )}
      </DialogContent>
    </Dialog>
  )
}
