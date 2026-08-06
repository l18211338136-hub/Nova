import { useTranslation } from 'react-i18next'
import { ShieldAlert, Code, AlertTriangle } from 'lucide-react'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Badge } from '@/components/ui/badge'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { type OperationLogDto } from '@/api/model'

interface OperationLogDetailDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  log: OperationLogDto | null
}

export function OperationLogDetailDialog({
  open,
  onOpenChange,
  log,
}: OperationLogDetailDialogProps) {
  const { t } = useTranslation()

  if (!log) return null

  const formatJson = (jsonStr?: string | null) => {
    if (!jsonStr) return '-'
    try {
      return JSON.stringify(JSON.parse(jsonStr), null, 2)
    } catch {
      return jsonStr
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className='max-w-3xl max-h-[85vh] overflow-y-auto'>
        <DialogHeader>
          <DialogTitle className='flex items-center gap-2 text-lg font-bold'>
            <span>{log.httpMethod}</span>
            {log.actionName && (
              <Badge variant='secondary' className='bg-primary/10 text-primary border border-primary/20 text-sm px-2 py-0.5 font-semibold'>
                {log.actionName}
              </Badge>
            )}
            <span className='font-mono text-muted-foreground text-sm ms-1'>{log.requestPath}</span>
            {log.hasSanitizedData && (
              <Badge variant='outline' className='bg-amber-100 text-amber-700 dark:bg-amber-950 dark:text-amber-300 gap-1 ms-auto'>
                <ShieldAlert className='h-3.5 w-3.5' />
                {t('含脱敏数据')}
              </Badge>
            )}
          </DialogTitle>
        </DialogHeader>

        <div className='space-y-4 my-2'>
          {/* 基本属性栅格 */}
          <div className='grid grid-cols-2 sm:grid-cols-4 gap-3 bg-muted/40 p-3 rounded-lg text-sm'>
            <div>
              <div className='text-xs text-muted-foreground'>{t('Trace ID')}</div>
              <div className='font-mono text-xs truncate' title={log.traceId || '-'}>{log.traceId || '-'}</div>
            </div>
            <div>
              <div className='text-xs text-muted-foreground'>{t('状态码 / 耗时')}</div>
              <div className='font-medium'>
                <Badge variant={log.statusCode && log.statusCode < 400 ? 'default' : 'destructive'} className='me-1.5'>
                  {log.statusCode || '-'}
                </Badge>
                <span>{log.elapsedMs} ms</span>
              </div>
            </div>
            <div>
              <div className='text-xs text-muted-foreground'>{t('客户端 IP')}</div>
              <div className='font-mono'>{log.clientIp || '-'}</div>
            </div>
            <div>
              <div className='text-xs text-muted-foreground'>{t('时间')}</div>
              <div className='whitespace-nowrap text-xs font-mono mt-0.5'>
                {log.createdAt ? new Date(log.createdAt).toLocaleString() : '-'}
              </div>
            </div>
          </div>

          {/* 选项卡内容 */}
          <Tabs defaultValue='request' className='w-full'>
            <TabsList className='grid w-full grid-cols-4'>
              <TabsTrigger type='button' value='request' className='gap-1'>
                <Code className='h-4 w-4' />
                {t('请求载荷')}
              </TabsTrigger>
              <TabsTrigger type='button' value='response' className='gap-1'>
                <Code className='h-4 w-4' />
                {t('响应载荷')}
              </TabsTrigger>
              <TabsTrigger type='button' value='sanitization' className='gap-1' disabled={!log.hasSanitizedData}>
                <ShieldAlert className='h-4 w-4' />
                {t('脱敏明细')} ({log.sanitizationDetails?.length || 0})
              </TabsTrigger>
              <TabsTrigger type='button' value='error' className='gap-1' disabled={!log.errorMessage && !log.exceptionStackTrace}>
                <AlertTriangle className='h-4 w-4' />
                {t('错误轨迹')}
              </TabsTrigger>
            </TabsList>

            <TabsContent value='request' className='mt-3'>
              <pre className='bg-zinc-950 text-zinc-100 dark:bg-zinc-900 p-4 rounded-md overflow-x-auto font-mono text-xs max-h-72'>
                {formatJson(log.requestPayload)}
              </pre>
            </TabsContent>

            <TabsContent value='response' className='mt-3'>
              <pre className='bg-zinc-950 text-zinc-100 dark:bg-zinc-900 p-4 rounded-md overflow-x-auto font-mono text-xs max-h-72'>
                {formatJson(log.responsePayload)}
              </pre>
            </TabsContent>

            <TabsContent value='sanitization' className='mt-3'>
              {log.sanitizationDetails && log.sanitizationDetails.length > 0 ? (
                <div className='border rounded-md overflow-hidden'>
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>{t('敏感字段名')}</TableHead>
                        <TableHead>{t('脱敏规则')}</TableHead>
                        <TableHead>{t('脱敏时间')}</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {log.sanitizationDetails.map((detail, idx) => (
                        <TableRow key={detail.id || idx}>
                          <TableCell className='font-mono font-semibold text-amber-600 dark:text-amber-400'>
                            {detail.fieldName}
                          </TableCell>
                          <TableCell>
                            <Badge variant='outline'>{detail.maskedRule || 'SensitiveKeyMask'}</Badge>
                          </TableCell>
                          <TableCell className='text-xs text-muted-foreground whitespace-nowrap'>
                            {detail.sanitizedAt ? new Date(detail.sanitizedAt).toLocaleString() : '-'}
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </div>
              ) : (
                <div className='text-center py-6 text-muted-foreground text-sm'>{t('未触发数据脱敏')}</div>
              )}
            </TabsContent>

            <TabsContent value='error' className='mt-3 space-y-2'>
              {log.errorMessage && (
                <div className='p-3 bg-red-50 border border-red-200 dark:bg-red-950/40 dark:border-red-900 rounded-md text-red-700 dark:text-red-300 font-medium text-sm'>
                  {log.errorMessage}
                </div>
              )}
              {log.exceptionStackTrace && (
                <pre className='bg-zinc-950 text-red-400 dark:bg-zinc-900 p-4 rounded-md overflow-x-auto font-mono text-xs max-h-60'>
                  {log.exceptionStackTrace}
                </pre>
              )}
            </TabsContent>
          </Tabs>
        </div>
      </DialogContent>
    </Dialog>
  )
}
