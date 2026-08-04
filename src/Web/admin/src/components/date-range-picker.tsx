import { format } from 'date-fns'
import { zhCN, enUS } from 'date-fns/locale'
import { useTranslation } from 'react-i18next'
import { Calendar as CalendarIcon } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'
import { Calendar } from '@/components/ui/calendar'
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover'

export type DateOp = 'between' | 'after' | 'before' | 'onOrAfter' | 'onOrBefore'

export type DateFilterValue = {
  op: DateOp
  from?: string   // ISO UTC string
  to?: string     // ISO UTC string
}

/** 将 Date 转为 UTC ISO 字符串（不含毫秒，避免 Npgsql Kind=Local 问题） */
function toUtcIso(date: Date): string {
  return new Date(Date.UTC(
    date.getFullYear(), date.getMonth(), date.getDate(),
    date.getHours(), date.getMinutes(), date.getSeconds()
  )).toISOString().replace(/\.\d{3}Z$/, 'Z')
}

type DateRangePickerProps = {
  value: DateFilterValue
  onSelect: (value: DateFilterValue) => void
  placeholder?: string
}

const OP_OPTIONS: Array<{ value: DateOp; labelKey: string; symbolKey: string }> = [
  { value: 'between', labelKey: 'Between', symbolKey: '–' },
  { value: 'onOrAfter', labelKey: 'On or after', symbolKey: '≥' },
  { value: 'onOrBefore', labelKey: 'On or before', symbolKey: '≤' },
  { value: 'after', labelKey: 'After', symbolKey: '>' },
  { value: 'before', labelKey: 'Before', symbolKey: '<' },
]

export function DateRangePicker({
  value,
  onSelect,
  placeholder,
}: DateRangePickerProps) {
  const { t, i18n } = useTranslation()
  const locale = i18n.language.startsWith('zh') ? zhCN : enUS

  // value.from / value.to 已经是 UTC ISO 字符串（toUtcIso 输出已带 Z），
  // 直接用 new Date 解析即可，切勿再拼 '+Z'，否则会变成 '...ZZ' 而解析成 Invalid Date。
  const fromDate = value.from ? new Date(value.from) : undefined
  const toDate = value.to ? new Date(value.to) : undefined

  const displayText = (() => {
    if (!fromDate && !toDate) return placeholder ?? t('Pick a date range')
    const fmt = (d: Date) => format(d, 'yyyy/MM/dd', { locale })
    switch (value.op) {
      case 'between':
        return fromDate && toDate ? `${fmt(fromDate)} – ${fmt(toDate)}`
          : fromDate ? fmt(fromDate)
          : fmt(toDate!)
      case 'onOrAfter':
      case 'after':
        return `${t(value.op === 'onOrAfter' ? '≥' : '>')} ${fmt(fromDate!)}`
      case 'onOrBefore':
      case 'before':
        return `${fmt(toDate!)} ${t(value.op === 'onOrBefore' ? '≤' : '<')}`
      default:
        return ''
    }
  })()

  const isRange = value.op === 'between'

  function handleCalendarSelect(date: Date | undefined) {
    if (!date) {
      onSelect({ op: value.op })
      return
    }
    const iso = toUtcIso(date)
    if (isRange) {
      onSelect({ ...value, from: iso, to: undefined })
    } else {
      if (value.op === 'after' || value.op === 'onOrAfter') {
        onSelect({ op: value.op, from: iso })
      } else {
        onSelect({ op: value.op, to: iso })
      }
    }
  }

  function handleRangeSelect(range: { from?: Date; to?: Date } | undefined) {
    if (!range || !range.from) {
      onSelect({ op: value.op })
      return
    }
    onSelect({
      op: value.op,
      from: toUtcIso(range.from),
      to: range.to ? toUtcIso(range.to) : undefined,
    })
  }

  function handleOpChange(op: DateOp) {
    // 切换操作符时清空已有值（避免语义矛盾）
    onSelect({ op })
  }

  return (
    <Popover>
      <PopoverTrigger asChild>
        <Button
          variant='outline'
          data-empty={!fromDate && !toDate}
          className='w-full justify-start text-start font-normal data-[empty=true]:text-muted-foreground h-8 text-xs mt-1'
        >
          <span>{displayText}</span>
          <CalendarIcon className='ms-auto h-4 w-4 opacity-50' />
        </Button>
      </PopoverTrigger>
      <PopoverContent className='w-auto p-0' align='start'>
        {/* 操作符切换条 — 紧贴日历顶部 */}
        <div className='flex items-center gap-0.5 border-b px-2 py-1.5 bg-muted/40'>
          {OP_OPTIONS.map((opt) => (
            <Button
              key={opt.value}
              variant={value.op === opt.value ? 'default' : 'ghost'}
              size='sm'
              className={cn(
                'h-6 px-2 text-xs font-normal',
                value.op === opt.value && 'pointer-events-none'
              )}
              onClick={() => handleOpChange(opt.value)}
              title={t(opt.labelKey)}
            >
              <span className='hidden sm:inline'>{t(opt.labelKey)}</span>
              <span className='sm:hidden'>{t(opt.symbolKey)}</span>
            </Button>
          ))}
        </div>

        {/* 日历 */}
        {isRange ? (
          <Calendar
            mode='range'
            captionLayout='dropdown'
            selected={{ from: fromDate, to: toDate }}
            onSelect={handleRangeSelect}
            numberOfMonths={2}
            // 允许选择未来日期（审计/通用日期筛选可能需要"晚于/早于某日"的未来值）
            disabled={(date: Date) => date < new Date('1900-01-01')}
          />
        ) : (
          <Calendar
            mode='single'
            captionLayout='dropdown'
            selected={fromDate ?? toDate}
            onSelect={handleCalendarSelect}
            numberOfMonths={2}
            // 允许选择未来日期（审计/通用日期筛选可能需要"晚于/早于某日"的未来值）
            disabled={(date: Date) => date < new Date('1900-01-01')}
          />
        )}
      </PopoverContent>
    </Popover>
  )
}
