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

  if (filterType === 'boolean') {
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
            <SelectItem value="true">{t('Active')}</SelectItem>
            <SelectItem value="false">{t('Inactive')}</SelectItem>
          </SelectContent>
        </Select>
      </div>
    )
  }

  if (filterType === 'number' || filterType === 'date') {
    return (
      <div className="flex space-x-2 mt-1">
        <Input
          type={filterType}
          value={(value as [any, any])?.[0] ?? ''}
          onChange={e =>
            setValue((old: any) => [e.target.value, old?.[1]])
          }
          placeholder={t('Min')}
          className="h-8 w-full min-w-[70px] text-xs"
        />
        <Input
          type={filterType}
          value={(value as [any, any])?.[1] ?? ''}
          onChange={e =>
            setValue((old: any) => [old?.[0], e.target.value])
          }
          placeholder={t('Max')}
          className="h-8 w-full min-w-[70px] text-xs"
        />
      </div>
    )
  }

  return (
    <div className="mt-1">
      <Input
        type="text"
        value={(value ?? '') as string}
        onChange={e => setValue(e.target.value)}
        placeholder={t('Filter...')}
        className="h-8 w-full min-w-[100px] text-xs"
      />
    </div>
  )
}
