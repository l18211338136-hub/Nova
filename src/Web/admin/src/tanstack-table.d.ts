import '@tanstack/react-table'

declare module '@tanstack/react-table' {
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  interface ColumnMeta<TData, TValue> {
    className?: string // apply to both th and td
    tdClassName?: string
    thClassName?: string
    filterType?: 'text' | 'number' | 'date' | 'boolean' | 'select'
    /** Options for select filter: array of { value, label } */
    selectOptions?: Array<{ value: string; label: string }>
    /** Options for boolean filter: { trueLabel, falseLabel } */
    booleanOptions?: { trueLabel: string; falseLabel: string }
    /** Placeholder text for text filter */
    filterPlaceholder?: string
  }
}
