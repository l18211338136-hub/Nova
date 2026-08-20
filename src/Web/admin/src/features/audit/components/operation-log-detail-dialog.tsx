import { useTranslation } from 'react-i18next'
import { ShieldAlert, Code, AlertTriangle } from 'lucide-react'
import { cn } from '@/lib/utils'
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
      <DialogContent className='max-w-3xl h-[80vh] flex flex-col overflow-hidden p-0 gap-0'>
        <DialogHeader className='ps-6 pe-12 pt-5 pb-4 border-b shrink-0 bg-background'>
          <DialogTitle className='flex items-center gap-2 text-lg font-bold min-w-0'>
            <span>{log.httpMethod}</span>
            {log.actionName && (
              <Badge variant='secondary' className='bg-primary/10 text-primary border border-primary/20 text-sm px-2 py-0.5 font-semibold'>
                {log.actionName}
              </Badge>
            )}
            <span className='font-mono text-muted-foreground text-sm ms-1 truncate' title={log.requestPath || ''}>{log.requestPath}</span>
            {log.hasSanitizedData && (
              <Badge variant='outline' className='bg-amber-100 text-amber-700 dark:bg-amber-950 dark:text-amber-300 gap-1 ms-auto shrink-0'>
                <ShieldAlert className='h-3.5 w-3.5' />
                {t('含脱敏数据')}
              </Badge>
            )}
          </DialogTitle>
        </DialogHeader>

        <div className='flex-1 flex flex-col min-h-0 p-6 space-y-4 overflow-hidden'>
          {/* 基本属性卡片 (双行精致布局) */}
          <div className='bg-muted/30 p-3.5 rounded-lg border border-border/40 text-xs shrink-0 space-y-3'>
            {/* 上排：链路追踪 ID 独占整行，单行且不折行 */}
            <div className='flex items-center gap-2 pb-2.5 border-b border-border/40 min-w-0'>
              <span className='text-[11px] font-semibold text-muted-foreground shrink-0'>{t('链路追踪 ID')}:</span>
              <code className='font-mono text-[11px] text-foreground/90 bg-background/80 px-2.5 py-1 rounded border border-border/50 whitespace-nowrap overflow-x-auto flex-1 select-all font-medium'>
                {log.traceId || '-'}
              </code>
            </div>

            {/* 下排：3 列等分指标 */}
            <div className='grid grid-cols-3 gap-4 text-xs items-center'>
              <div className='flex flex-col gap-1 min-w-0'>
                <span className='text-[11px] font-medium text-muted-foreground'>{t('状态码 / 耗时')}</span>
                <div className='flex items-center gap-1.5 font-mono text-xs text-nowrap'>
                  <Badge
                    variant='outline'
                    className={cn(
                      'px-1.5 py-0 text-[11px] font-semibold border',
                      log.statusCode && log.statusCode < 400
                        ? 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border-emerald-500/20'
                        : 'bg-rose-500/10 text-rose-600 dark:text-rose-400 border-rose-500/20'
                    )}
                  >
                    {log.statusCode || '-'}
                  </Badge>
                  <span className='font-medium text-foreground/90'>{log.elapsedMs} ms</span>
                </div>
              </div>

              <div className='flex flex-col gap-1 min-w-0'>
                <span className='text-[11px] font-medium text-muted-foreground'>{t('客户端 IP')}</span>
                <span className='font-mono text-xs text-nowrap text-foreground/90'>{log.clientIp || '-'}</span>
              </div>

              <div className='flex flex-col gap-1 min-w-0'>
                <span className='text-[11px] font-medium text-muted-foreground'>{t('时间')}</span>
                <span className='font-mono text-xs text-nowrap text-foreground/90'>
                  {log.createdAt ? new Date(log.createdAt).toLocaleString() : '-'}
                </span>
              </div>
            </div>
          </div>

          {/* 选项卡内容 */}
          <Tabs defaultValue='request' className='flex-1 flex flex-col min-h-0 w-full'>
            <TabsList className='grid w-full grid-cols-4 shrink-0'>
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

            <TabsContent value='request' className='flex-1 min-h-0 mt-3 data-[state=inactive]:hidden'>
              <pre className='bg-zinc-950 text-zinc-100 dark:bg-zinc-900 p-4 rounded-md overflow-auto font-mono text-xs h-full border'>
                {formatJson(log.requestPayload)}
              </pre>
            </TabsContent>

            <TabsContent value='response' className='flex-1 min-h-0 mt-3 data-[state=inactive]:hidden'>
              <pre className='bg-zinc-950 text-zinc-100 dark:bg-zinc-900 p-4 rounded-md overflow-auto font-mono text-xs h-full border'>
                {formatJson(log.responsePayload)}
              </pre>
            </TabsContent>

            <TabsContent value='sanitization' className='flex-1 min-h-0 mt-3 data-[state=inactive]:hidden'>
              {log.sanitizationDetails && log.sanitizationDetails.length > 0 ? (
                <div className='border rounded-md overflow-auto h-full'>
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
                <div className='flex items-center justify-center h-full border rounded-md text-muted-foreground text-sm'>{t('未触发数据脱敏')}</div>
              )}
            </TabsContent>

            <TabsContent value='error' className='flex-1 min-h-0 mt-3 overflow-auto space-y-2 data-[state=inactive]:hidden'>
              {log.errorMessage && (
                <div className='p-3 bg-red-50 border border-red-200 dark:bg-red-950/40 dark:border-red-900 rounded-md text-red-700 dark:text-red-300 font-medium text-sm'>
                  {log.errorMessage}
                </div>
              )}
              {log.exceptionStackTrace && (
                <pre className='bg-zinc-950 text-red-400 dark:bg-zinc-900 p-4 rounded-md overflow-auto font-mono text-xs border'>
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
