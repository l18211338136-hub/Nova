import { type Column } from '@tanstack/react-table'
import { Input } from '@/components/ui/input'
import { useTranslation } from 'react-i18next'
import { useState, useEffect } from 'react'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { DateRangePicker, type DateFilterValue } from '@/components/date-range-picker'
import { NumberRangePicker, type NumberFilterValue } from '@/components/number-range-picker'

export interface DataTableColumnFilterProps<TData, TValue> {
  column: Column<TData, TValue>
}

// A generic column filter component that reads column.columnDef.meta?.filterType
// filterType can be 'text' | 'number' | 'date' | 'select' | 'boolean'
export function DataTableColumnFilter<TData, TValue>({ column }: DataTableColumnFilterProps<TData, TValue>) {
  const { t } = useTranslation()
  const columnFilterValue = column.getFilterValue()
  const filterType = (column.columnDef.meta as any)?.filterType ?? 'text'

  // Use local state for debouncing typing (text filter only)
  const [value, setValue] = useState(columnFilterValue)

  useEffect(() => {
    setValue(columnFilterValue)
  }, [columnFilterValue])

  useEffect(() => {
    const timeout = setTimeout(() => {
      if (value !== column.getFilterValue()) {
        column.setFilterValue(value)
      }
    }, 500)
    return () => clearTimeout(timeout)
  }, [value, column])

  // --- select: standard Select dropdown ---
  if (filterType === 'select') {
    const meta = (column.columnDef.meta as Record<string, any>) ?? {}
    const options: Array<{ value: string; label: string }> = meta.selectOptions ?? []
    return (
      <div className='mt-1'>
        <Select
          value={columnFilterValue !== undefined && columnFilterValue !== null ? String(columnFilterValue) : 'all'}
          onValueChange={(val) => {
            if (val === 'all') {
              column.setFilterValue(undefined)
            } else {
              column.setFilterValue(val)
            }
          }}
        >
          <SelectTrigger className='h-8 w-full text-xs'>
            <SelectValue placeholder={t('All')} />
          </SelectTrigger>
          <SelectContent side='bottom' align='start'>
            <SelectItem value='all'>{t('All')}</SelectItem>
            {options.map((opt) => (
              <SelectItem key={opt.value} value={opt.value}>
                {opt.label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>
    )
  }

  // --- boolean: standard Select dropdown ---
  if (filterType === 'boolean') {
    const meta = (column.columnDef.meta as Record<string, any>) ?? {}
    const boolOpts = meta.booleanOptions ?? {}
    const trueLabel = boolOpts.trueLabel ?? t('Active')
    const falseLabel = boolOpts.falseLabel ?? t('Inactive')
    return (
      <div className='mt-1'>
        <Select
          value={columnFilterValue !== undefined && columnFilterValue !== null ? String(columnFilterValue) : 'all'}
          onValueChange={(val) => {
            if (val === 'all') {
              column.setFilterValue(undefined)
            } else {
              column.setFilterValue(val === 'true')
            }
          }}
        >
          <SelectTrigger className='h-8 w-full text-xs'>
            <SelectValue placeholder={t('All')} />
          </SelectTrigger>
          <SelectContent side='bottom' align='start'>
            <SelectItem value='all'>{t('All')}</SelectItem>
            <SelectItem value='true'>{trueLabel}</SelectItem>
            <SelectItem value='false'>{falseLabel}</SelectItem>
          </SelectContent>
        </Select>
      </div>
    )
  }

  if (filterType === 'number') {
    const nf = (value as NumberFilterValue | undefined) ?? {
      kind: 'number' as const,
      op: 'between' as const,
    }
    return (
      <NumberRangePicker
        value={nf}
        onSelect={(v) => {
          setValue(v)
          column.setFilterValue(v)
        }}
      />
    )
  }

  if (filterType === 'date') {
    const dfv = (value as DateFilterValue | undefined) ?? { op: 'between' as const }
    return (
      <DateRangePicker
        value={dfv}
        onSelect={(v) => {
          setValue(v)
          column.setFilterValue(v)
        }}
      />
    )
  }

  // Default: text input with debounce
  return (
    <div className='mt-1'>
      <Input
        type='text'
        value={(value ?? '') as string}
        onChange={e => setValue(e.target.value)}
        placeholder={(column.columnDef.meta as Record<string, any>)?.filterPlaceholder ?? t('Filter...')}
        className='h-8 w-full min-w-[100px] text-xs'
      />
    </div>
  )
}
