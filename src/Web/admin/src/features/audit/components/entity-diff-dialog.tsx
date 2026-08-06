import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { History, ArrowRight, User, Calendar, Tag, PlusCircle, MinusCircle } from 'lucide-react'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Badge } from '@/components/ui/badge'
import { ScrollArea } from '@/components/ui/scroll-area'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { useGetEntityChanges } from '@/api/endpoints/audit'
import type { EntityChangeLogDto } from '@/api/model'

interface EntityDiffDialogProps {
  entityType?: string
  entityId?: string
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function EntityDiffDialog({
  entityType,
  entityId,
  open,
  onOpenChange,
}: EntityDiffDialogProps) {
  const { t } = useTranslation()
  const [selectedLogId, setSelectedLogId] = useState<string | null>(null)

  const { data: response, isLoading } = useGetEntityChanges(
    {
      page: 1,
      pageSize: 50,
      ...(entityType && { entityType }),
      ...(entityId && { entityId }),
    },
    {
      query: {
        enabled: open && !!entityType && !!entityId,
      },
    }
  )

  const items = (response?.data?.items as EntityChangeLogDto[]) || []
  const activeLog = items.find((x) => x.id === selectedLogId) || items[0]

  const getChangeTypeLabel = (type?: string) => {
    if (!type) return '-'
    const map: Record<string, string> = {
      Added: t('新增 (Added)'),
      Modified: t('修改 (Modified)'),
      Deleted: t('删除 (Deleted)'),
    }
    return map[type] || type
  }

  const getEntityTypeLabel = (type?: string) => {
    if (!type) return '-'
    const map: Record<string, string> = {
      User: t('用户 (User)'),
      Role: t('角色 (Role)'),
      Tenant: t('租户 (Tenant)'),
      Organization: t('组织 (Organization)'),
      Permission: t('权限 (Permission)'),
    }
    return map[type] || type
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className='sm:max-w-5xl w-[92vw] h-[85vh] flex flex-col p-0 overflow-hidden'>
        <DialogHeader className='px-6 pt-5 pb-4 border-b shrink-0 bg-background/95 backdrop-blur z-10'>
          <div className='flex items-center gap-2'>
            <History className='h-5 w-5 text-primary' />
            <DialogTitle className='text-xl font-bold'>
              {t('数据行级变更追溯 (Visual Diff)')}
            </DialogTitle>
          </div>
          <DialogDescription className='text-xs text-muted-foreground mt-1.5 flex items-center gap-3'>
            <span>{t('Entity Type:')} <Badge variant='secondary' className='font-mono font-normal ml-1'>{getEntityTypeLabel(entityType)}</Badge></span>
            <span>{t('Identifier:')} <span className='font-mono font-semibold text-foreground ml-1'>{entityId}</span></span>
          </DialogDescription>
        </DialogHeader>

        {isLoading ? (
          <div className='flex items-center justify-center flex-1 text-muted-foreground text-sm'>
            {t('加载行级数据变更记录中...')}
          </div>
        ) : items.length === 0 ? (
          <div className='flex items-center justify-center flex-1 text-muted-foreground text-sm'>
            {t('暂无该实体的字段级修改历史记录')}
          </div>
        ) : (
          <div className='flex flex-1 overflow-hidden min-h-0'>
            {/* 左侧 Timeline 版本历史点列表 (固定 280px 宽度) */}
            <div className='w-72 border-r bg-muted/20 p-3 overflow-hidden flex flex-col shrink-0'>
              <h4 className='text-xs font-semibold text-muted-foreground uppercase tracking-wider mb-2.5 px-1'>
                {t('变更版本时间线')} ({items.length})
              </h4>
              <ScrollArea className='flex-1 pe-2'>
                <div className='space-y-2'>
                  {items.map((log) => {
                    const isSelected = (activeLog?.id || items[0]?.id) === log.id
                    return (
                      <button
                        key={log.id}
                        onClick={() => setSelectedLogId(log.id!)}
                        className={`w-full text-start p-3 rounded-lg border text-xs transition-all ${
                          isSelected
                            ? 'bg-card border-primary ring-1 ring-primary shadow-sm'
                            : 'bg-background hover:bg-accent border-border/60'
                        }`}
                      >
                        <div className='flex items-center justify-between gap-1 mb-1.5'>
                          <Badge
                            className={
                              log.changeType === 'Added'
                                ? 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border-emerald-500/20'
                                : log.changeType === 'Modified'
                                ? 'bg-blue-500/10 text-blue-600 dark:text-blue-400 border-blue-500/20'
                                : 'bg-rose-500/10 text-rose-600 dark:text-rose-400 border-rose-500/20'
                            }
                          >
                            {getChangeTypeLabel(log.changeType)}
                          </Badge>
                          <span className='text-[10px] text-muted-foreground font-mono flex items-center gap-1 shrink-0'>
                            <Calendar className='h-3 w-3' />
                            {log.createdAt ? new Date(log.createdAt).toLocaleTimeString() : '-'}
                          </span>
                        </div>

                        <div className='flex items-center gap-1.5 text-muted-foreground font-mono text-[11px] truncate mt-2'>
                          <User className='h-3.5 w-3.5 shrink-0 text-muted-foreground/70' />
                          <span className='truncate font-medium'>{log.operatorName || 'System'}</span>
                        </div>
                      </button>
                    )
                  })}
                </div>
              </ScrollArea>
            </div>

            {/* 右侧 Visual Diff 表明细对比 (占用余下所有 100% 空间) */}
            <div className='flex-1 p-5 overflow-hidden flex flex-col min-w-0 bg-background'>
              {activeLog && (
                <>
                  <div className='flex items-center justify-between mb-3 text-xs bg-muted/40 p-3 rounded-lg border shrink-0'>
                    <div className='flex items-center gap-2'>
                      <Tag className='h-4 w-4 text-primary' />
                      <span className='font-semibold'>{t('操作类型')}: {getChangeTypeLabel(activeLog.changeType)}</span>
                    </div>
                    <div className='text-muted-foreground font-mono'>
                      {activeLog.createdAt ? new Date(activeLog.createdAt).toLocaleString() : '-'}
                    </div>
                  </div>

                  <div className='flex-1 border rounded-lg overflow-hidden bg-card min-h-0 flex flex-col'>
                    <ScrollArea className='h-full w-full'>
                      <Table className='w-full border-collapse'>
                        <TableHeader className='bg-muted/60 sticky top-0 z-10 shadow-sm'>
                          <TableRow className='hover:bg-transparent'>
                            <TableHead className='w-[180px] font-bold text-foreground'>{t('变更字段 / 属性')}</TableHead>
                            <TableHead className='w-[42%] text-rose-600 dark:text-rose-400 font-bold'>{t('修改前 (Original Value)')}</TableHead>
                            <TableHead className='w-[36px] p-0 text-center'></TableHead>
                            <TableHead className='w-[42%] text-emerald-600 dark:text-emerald-400 font-bold'>{t('修改后 (New Value)')}</TableHead>
                          </TableRow>
                        </TableHeader>
                        <TableBody>
                          {activeLog.propertyChanges && activeLog.propertyChanges.length > 0 ? (
                            activeLog.propertyChanges.map((change) => {
                              const isPermissionOrMenu = change.propertyName === 'Permission' || change.propertyName === 'Menu' || change.propertyName === 'UserRole'

                              return (
                                <TableRow key={change.id} className='hover:bg-muted/30'>
                                  <TableCell className='font-mono text-xs font-semibold align-top py-3 break-all'>
                                    <div className='flex items-center gap-1.5'>
                                      {change.propertyDisplayName || change.propertyName}
                                    </div>
                                  </TableCell>
                                  <TableCell className='font-mono text-xs bg-rose-500/5 text-rose-700 dark:text-rose-300 border-r align-top py-3 break-all'>
                                    {change.originalValue ? (
                                      <span className='line-through opacity-85 inline-flex items-center gap-1'>
                                        {isPermissionOrMenu && <MinusCircle className='h-3.5 w-3.5 text-rose-500 shrink-0' />}
                                        {change.originalValue}
                                      </span>
                                    ) : (
                                      <span className='italic opacity-50 font-normal'>(null)</span>
                                    )}
                                  </TableCell>
                                  <TableCell className='p-0 text-center text-muted-foreground align-top py-3'>
                                    <ArrowRight className='h-4 w-4 mx-auto text-muted-foreground/70' />
                                  </TableCell>
                                  <TableCell className='font-mono text-xs bg-emerald-500/5 text-emerald-700 dark:text-emerald-300 font-medium align-top py-3 break-all'>
                                    {change.newValue ? (
                                      <span className='inline-flex items-center gap-1'>
                                        {isPermissionOrMenu && <PlusCircle className='h-3.5 w-3.5 text-emerald-500 shrink-0' />}
                                        {change.newValue}
                                      </span>
                                    ) : (
                                      <span className='italic opacity-50 font-normal'>(null)</span>
                                    )}
                                  </TableCell>
                                </TableRow>
                              )
                            })
                          ) : (
                            <TableRow>
                              <TableCell colSpan={4} className='h-32 text-center text-muted-foreground text-xs'>
                                {t('无检测到的字段值变动')}
                              </TableCell>
                            </TableRow>
                          )}
                        </TableBody>
                      </Table>
                    </ScrollArea>
                  </div>
                </>
              )}
            </div>
          </div>
        )}
      </DialogContent>
    </Dialog>
  )
}
