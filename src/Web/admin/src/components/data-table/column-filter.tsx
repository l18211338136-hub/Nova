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
// filterType can be 'text' | 'number' | 'date'
export function DataTableColumnFilter<TData, TValue>({ column }: DataTableColumnFilterProps<TData, TValue>) {
  const { t } = useTranslation()
  const columnFilterValue = column.getFilterValue()
  const filterType = (column.columnDef.meta as any)?.filterType ?? 'text'

  // Use local state for debouncing typing
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

  if (filterType === 'select') {
    const meta = (column.columnDef.meta as Record<string, any>) ?? {}
    const options: Array<{ value: string; label: string }> = meta.selectOptions ?? []
    return (
      <div className="mt-1">
        <Select
          value={value !== undefined && value !== null ? String(value) : "all"}
          onValueChange={(val) => {
            if (val === "all") {
              setValue(undefined)
              column.setFilterValue(undefined)
            } else {
              setValue(val)
              column.setFilterValue(val)
            }
          }}
        >
          <SelectTrigger className="h-8 w-full text-xs">
            <SelectValue placeholder={t('All')} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">{t('All')}</SelectItem>
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

  if (filterType === 'boolean') {
    const meta = (column.columnDef.meta as Record<string, any>) ?? {}
    const boolOpts = meta.booleanOptions ?? {}
    const trueLabel = boolOpts.trueLabel ?? t('Active')
    const falseLabel = boolOpts.falseLabel ?? t('Inactive')
    return (
      <div className="mt-1">
        <Select
          value={value !== undefined && value !== null ? String(value) : "all"}
          onValueChange={(val) => {
            if (val === "all") {
              setValue(undefined)
              column.setFilterValue(undefined)
            } else {
              const boolVal = val === "true"
              setValue(boolVal)
              column.setFilterValue(boolVal)
            }
          }}
        >
          <SelectTrigger className="h-8 w-full text-xs">
            <SelectValue placeholder={t('All')} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">{t('All')}</SelectItem>
            <SelectItem value="true">{trueLabel}</SelectItem>
            <SelectItem value="false">{falseLabel}</SelectItem>
          </SelectContent>
        </Select>
      </div>
    )
  }

  if (filterType === 'number') {
    // NumberFilterValue: { kind:'number', op:'between'|'after'|'before'|'onOrAfter'|'onOrBefore', from?: number, to?: number }
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
    // DateFilterValue: { op: 'between'|'after'|'before'|'onOrAfter'|'onOrBefore', from?: string, to?: string }
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

  return (
    <div className="mt-1">
      <Input
        type="text"
        value={(value ?? '') as string}
        onChange={e => setValue(e.target.value)}
        placeholder={(column.columnDef.meta as Record<string, any>)?.filterPlaceholder ?? t('Filter...')}
        className="h-8 w-full min-w-[100px] text-xs"
      />
    </div>
  )
}
