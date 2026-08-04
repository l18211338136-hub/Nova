import { type ColumnFilter, type SortingState } from '@tanstack/react-table'

/** 日期筛选值（DateRangePicker 输出） */
interface DateFilterObj {
  op: string
  from?: string
  to?: string
}

function isDateFilterObj(val: any): val is DateFilterObj {
  return val && typeof val === 'object' && typeof val.op === 'string'
}

export function buildODataFilter(columnFilters: ColumnFilter[]): string | undefined {
  if (!columnFilters || columnFilters.length === 0) return undefined

  const filters = columnFilters.map((f) => {
    const field = f.id.charAt(0).toUpperCase() + f.id.slice(1)
    const val = f.value

    // Number filter with operator (from NumberRangePicker)
    // 必须先于日期判定：数字对象也带 op 字段，会被 isDateFilterObj 误判
    if (val && typeof val === 'object' && (val as any).kind === 'number') {
      const n = (v: any) =>
        v === undefined || v === null || v === '' ? undefined : Number(v)
      const conditions: string[] = []
      switch ((val as any).op) {
        case 'between':
          if (n((val as any).from) !== undefined)
            conditions.push(`${field} ge ${n((val as any).from)}`)
          if (n((val as any).to) !== undefined)
            conditions.push(`${field} le ${n((val as any).to)}`)
          break
        case 'onOrAfter':
          if (n((val as any).from) !== undefined)
            conditions.push(`${field} ge ${n((val as any).from)}`)
          break
        case 'after':
          if (n((val as any).from) !== undefined)
            conditions.push(`${field} gt ${n((val as any).from)}`)
          break
        case 'onOrBefore':
          if (n((val as any).to) !== undefined)
            conditions.push(`${field} le ${n((val as any).to)}`)
          break
        case 'before':
          if (n((val as any).to) !== undefined)
            conditions.push(`${field} lt ${n((val as any).to)}`)
          break
      }
      return conditions.length > 0 ? `(${conditions.join(' and ')})` : ''
    }

    // Date filter with operator (from DateRangePicker)
    if (isDateFilterObj(val)) {
      const conditions: string[] = []
      switch (val.op) {
        case 'between':
          if (val.from) conditions.push(`${field} ge ${formatODataValue(val.from, false)}`)
          if (val.to) conditions.push(`${field} le ${formatODataValue(val.to, true)}`)
          break
        case 'onOrAfter':
          if (val.from) conditions.push(`${field} ge ${formatODataValue(val.from, false)}`)
          break
        case 'after':
          if (val.from) conditions.push(`${field} gt ${formatODataValue(val.from, false)}`)
          break
        case 'onOrBefore':
          if (val.to) conditions.push(`${field} le ${formatODataValue(val.to, true)}`)
          break
        case 'before':
          if (val.to) conditions.push(`${field} lt ${formatODataValue(val.to, true)}`)
          break
      }
      return conditions.length > 0 ? `(${conditions.join(' and ')})` : ''
    }

    // Handle range filter (legacy array [min, max])
    if (Array.isArray(val) && val.length === 2) {
      const [min, max] = val
      const conditions = []
      if (min !== undefined && min !== null && min !== '') {
        conditions.push(`${field} ge ${formatODataValue(min, false)}`)
      }
      if (max !== undefined && max !== null && max !== '') {
        conditions.push(`${field} le ${formatODataValue(max, true)}`)
      }
      return conditions.length > 0 ? `(${conditions.join(' and ')})` : ''
    }

    // Handle Faceted filter (array of selected values)
    if (Array.isArray(val)) {
      const orConditions = val.map(v => `${field} eq ${formatODataValue(v)}`)
      return orConditions.length > 0 ? `(${orConditions.join(' or ')})` : ''
    }

    // Handle object { min, max } (legacy)
    if (val !== null && typeof val === 'object') {
      const range = val as { min?: any; max?: any }
      const conditions = []
      if (range.min !== undefined && range.min !== null && range.min !== '') {
        conditions.push(`${field} ge ${formatODataValue(range.min, false)}`)
      }
      if (range.max !== undefined && range.max !== null && range.max !== '') {
        conditions.push(`${field} le ${formatODataValue(range.max, true)}`)
      }
      return conditions.length > 0 ? `(${conditions.join(' and ')})` : ''
    }

    // Exact match for boolean or number
    if (typeof val === 'boolean' || typeof val === 'number') {
      return `${field} eq ${val}`
    }

    // String search (fuzzy)
    return `contains(${field}, '${String(val).replace(/'/g, "''")}')`
  })

  const validFilters = filters.filter(Boolean)
  return validFilters.length > 0 ? validFilters.join(' and ') : undefined
}

export function buildODataOrderBy(sorting: SortingState): string | undefined {
  if (!sorting || sorting.length === 0) return undefined
  return sorting.map((s) => {
    const field = s.id.charAt(0).toUpperCase() + s.id.slice(1)
    return `${field} ${s.desc ? 'desc' : 'asc'}`
  }).join(', ')
}

function formatODataValue(val: any, isRangeMax: boolean = false): string {
  if (typeof val === 'string') {
    // If it's a date-only string like YYYY-MM-DD
    if (/^\d{4}-\d{2}-\d{2}$/.test(val)) {
      // For OData V4 DateTimeOffset, append time + Z for UTC
      const time = isRangeMax ? 'T23:59:59Z' : 'T00:00:00Z'
      return val + time
    }
    // If it's already an ISO date string with Z (UTC), pass through
    if (/^\d{4}-\d{2}-\d{2}T.*Z$/.test(val)) {
      return val
    }
    // ISO date without Z — treat as local, convert to UTC by appending Z
    // (backend Npgsql requires UTC Kind for timestamp with time zone)
    if (/^\d{4}-\d{2}-\d{2}T/.test(val)) {
      return val.includes('Z') ? val : val + 'Z'
    }
    return `'${val.replace(/'/g, "''")}'`
  }
  return String(val)
}
