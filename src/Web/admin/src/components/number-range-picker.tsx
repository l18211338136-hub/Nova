import { useTranslation } from 'react-i18next'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'

export type NumberOp = 'between' | 'after' | 'before' | 'onOrAfter' | 'onOrBefore'

/** 数字筛选值（NumberRangePicker 输出），与 DateFilterValue 同构，多一个 kind 以便 odata 区分 */
export type NumberFilterValue = {
  kind: 'number'
  op: NumberOp
  from?: number
  to?: number
}

const OP_OPTIONS: Array<{ value: NumberOp; labelKey: string; symbolKey: string }> = [
  { value: 'between', labelKey: 'Number between', symbolKey: '–' },
  { value: 'onOrAfter', labelKey: 'Number at least', symbolKey: '≥' },
  { value: 'onOrBefore', labelKey: 'Number at most', symbolKey: '≤' },
  { value: 'after', labelKey: 'Number more than', symbolKey: '>' },
  { value: 'before', labelKey: 'Number less than', symbolKey: '<' },
]

function fmtNum(n: number | undefined): string {
  return n === undefined || Number.isNaN(n) ? '' : String(n)
}

export function NumberRangePicker({
  value,
  onSelect,
}: {
  value: NumberFilterValue
  onSelect: (value: NumberFilterValue) => void
}) {
  const { t } = useTranslation()

  const isRange = value.op === 'between'

  function handleOpChange(op: NumberOp) {
    // 切换操作符时清空已有值（避免单边/范围语义矛盾）
    onSelect({ kind: 'number', op })
  }

  function parseNum(s: string): number | undefined {
    const trimmed = s.trim()
    if (trimmed === '') return undefined
    const n = Number(trimmed)
    return Number.isNaN(n) ? undefined : n
  }

  function handleFromChange(s: string) {
    const n = parseNum(s)
    if (isRange) {
      onSelect({ kind: 'number', op: value.op, from: n, to: value.to })
    } else {
      onSelect({ kind: 'number', op: value.op, from: n })
    }
  }

  function handleToChange(s: string) {
    const n = parseNum(s)
    if (isRange) {
      onSelect({ kind: 'number', op: value.op, from: value.from, to: n })
    } else {
      onSelect({ kind: 'number', op: value.op, to: n })
    }
  }

  return (
    <div className='mt-1'>
      {/* 操作符切换条 — 中文标签较长，允许换行 */}
      <div className='flex flex-wrap items-center gap-1 mb-2'>
        {OP_OPTIONS.map((opt) => (
          <Button
            key={opt.value}
            variant={value.op === opt.value ? 'default' : 'outline'}
            size='sm'
            className={cn(
              'h-6 px-2 text-xs font-normal',
              value.op === opt.value && 'pointer-events-none'
            )}
            onClick={() => handleOpChange(opt.value)}
            title={t(opt.labelKey)}
          >
            {t(opt.labelKey)}
          </Button>
        ))}
      </div>

      {isRange ? (
        <div className='flex space-x-2'>
          <Input
            type='number'
            value={fmtNum(value.from)}
            onChange={(e) => handleFromChange(e.target.value)}
            placeholder={t('Min')}
            className='h-8 w-full min-w-[70px] text-xs'
          />
          <Input
            type='number'
            value={fmtNum(value.to)}
            onChange={(e) => handleToChange(e.target.value)}
            placeholder={t('Max')}
            className='h-8 w-full min-w-[70px] text-xs'
          />
        </div>
      ) : (
        <Input
          type='number'
          value={fmtNum(
            value.op === 'after' || value.op === 'onOrAfter' ? value.from : value.to
          )}
          onChange={(e) =>
            value.op === 'after' || value.op === 'onOrAfter'
              ? handleFromChange(e.target.value)
              : handleToChange(e.target.value)
          }
          placeholder={t('Value')}
          className='h-8 w-full min-w-[70px] text-xs'
        />
      )}
    </div>
  )
}
