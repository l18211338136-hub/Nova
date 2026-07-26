import { type ColumnFilter, type SortingState } from '@tanstack/react-table'

export function buildODataFilter(columnFilters: ColumnFilter[]): string | undefined {
  if (!columnFilters || columnFilters.length === 0) return undefined

  const filters = columnFilters.map((f) => {
    const field = f.id.charAt(0).toUpperCase() + f.id.slice(1)
    const val = f.value

    // Handle range filter (e.g. for numbers or dates)
    // The filter value is expected to be an object: { min?: any, max?: any }
    // Or an array: [min, max]
    if (Array.isArray(val) && val.length === 2) {
      const [min, max] = val
      const conditions = []
      if (min !== undefined && min !== null && min !== '') {
        conditions.push(`${field} ge ${formatODataValue(min, false)}`)
      }
      if (max !== undefined && max !== null && max !== '') {
        // For max date, if it's a date only, we might want to append time, but we'll pass it to backend
        conditions.push(`${field} le ${formatODataValue(max, true)}`)
      }
      return conditions.length > 0 ? `(${conditions.join(' and ')})` : ''
    }

    // Handle Faceted filter (array of selected values)
    if (Array.isArray(val)) {
      const orConditions = val.map(v => `${field} eq ${formatODataValue(v)}`)
      return orConditions.length > 0 ? `(${orConditions.join(' or ')})` : ''
    }

    // Handle object { min, max }
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
    // If it's a date string like YYYY-MM-DD
    if (/^\d{4}-\d{2}-\d{2}$/.test(val)) {
      // In OData V4, Edm.DateTimeOffset requires Z
      // If it's a max value for a date range, we might want to include the whole day, but for now just append T00:00:00Z
      const time = isRangeMax ? 'T23:59:59Z' : 'T00:00:00Z'
      return val + time
    }
    // If it's already an ISO date string
    if (/^\d{4}-\d{2}-\d{2}T/.test(val)) {
      return val
    }
    return `'${val.replace(/'/g, "''")}'`
  }
  return String(val)
}
