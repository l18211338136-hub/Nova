import { useEffect, useState, Fragment } from 'react'
import { useTranslation } from 'react-i18next'
import {
  type SortingState,
  type VisibilityState,
  flexRender,
  getCoreRowModel,
  getFacetedRowModel,
  getFacetedUniqueValues,
  getFilteredRowModel,
  getSortedRowModel,
  useReactTable,
} from '@tanstack/react-table'
import { cn } from '@/lib/utils'
import { type NavigateFn, useTableUrlState } from '@/hooks/use-table-url-state'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { DataTablePagination, DataTableToolbar } from '@/components/data-table'
import { type AuthAuditLogDto as AuthAuditLog } from '@/api/model'
import { useAuthAuditLogs } from '@/api/endpoints/audit'
import { useAuditColumns } from './audit-columns'
import { buildODataFilter, buildODataOrderBy } from '@/lib/odata'
import { type DateFilterValue } from '@/components/date-range-picker'

type DataTableProps = {
  search: Record<string, unknown>
  navigate: NavigateFn
}

export function AuditTable({ search, navigate }: DataTableProps) {
  const { t } = useTranslation()
  const columns = useAuditColumns()
  const [rowSelection, setRowSelection] = useState({})
  const [columnVisibility, setColumnVisibility] = useState<VisibilityState>({})
  const [sorting, setSorting] = useState<SortingState>([])

  const {
    columnFilters,
    onColumnFiltersChange,
    pagination,
    onPaginationChange,
    ensurePageInRange,
  } = useTableUrlState({
    search,
    navigate,
    pagination: { defaultPage: 1, defaultPageSize: 10 },
    globalFilter: { enabled: false },
    columnFilters: [
      { columnId: 'eventType', searchKey: 'eventType', type: 'string' },
      { columnId: 'account', searchKey: 'account', type: 'string' },
      { columnId: 'success', searchKey: 'success', type: 'boolean' },
      { columnId: 'ipAddress', searchKey: 'ipAddress', type: 'string' },
      {
        columnId: 'occurredOn',
        searchKey: 'occurredOn',
        type: 'string' as const,
        serialize: (v: unknown) => {
          if (!v || typeof v !== 'object') return ''
          const d = v as DateFilterValue
          // JSON 序列化：{"op":"between","from":"...","to":"..."}
          return JSON.stringify(d)
        },
        deserialize: (v: unknown) => {
          if (typeof v !== 'string' || !v.trim()) return { op: 'between' as const }
          try { return JSON.parse(v) as DateFilterValue }
          catch { return { op: 'between' as const } }
        },
      },
    ],
  })

  const $filter = buildODataFilter(columnFilters)
  const $orderby = buildODataOrderBy(sorting) || 'OccurredOn desc'

  const { data: apiResponse, isLoading, isError } = useAuthAuditLogs({
    query: {
      queryKey: ['audit-logs', pagination, columnFilters, sorting],
    },
    request: {
      params: {
        $count: true,
        $skip: pagination.pageIndex * pagination.pageSize,
        $top: pagination.pageSize,
        ...($filter && { $filter }),
        ...($orderby && { $orderby }),
      },
    },
  })

  const items = (apiResponse?.data?.items as AuthAuditLog[]) ?? []
  const totalCount = apiResponse?.data?.total ?? 0
  const pageCount = Math.ceil(totalCount / pagination.pageSize)

  const table = useReactTable({
    data: items,
    columns,
    pageCount,
    manualPagination: true,
    manualFiltering: true,
    manualSorting: true,
    state: {
      sorting,
      pagination,
      rowSelection,
      columnFilters,
      columnVisibility,
    },
    onPaginationChange,
    onColumnFiltersChange,
    onRowSelectionChange: setRowSelection,
    onSortingChange: setSorting,
    onColumnVisibilityChange: setColumnVisibility,
    getCoreRowModel: getCoreRowModel(),
    getFilteredRowModel: getFilteredRowModel(),
    getSortedRowModel: getSortedRowModel(),
    getFacetedRowModel: getFacetedRowModel(),
    getFacetedUniqueValues: getFacetedUniqueValues(),
  })

  useEffect(() => {
    ensurePageInRange(table.getPageCount())
  }, [table, ensurePageInRange])

  return (
    <div
      className={cn(
        'max-sm:has-[div[role="toolbar"]]:mb-16',
        'flex flex-1 flex-col gap-4'
      )}
    >
      <DataTableToolbar table={table} hideSearch={true} filters={[]} />
      <div className='overflow-hidden rounded-md border'>
        <Table className='table-fixed'>
          <TableHeader>
            {table.getHeaderGroups().map((headerGroup) => (
              <Fragment key={headerGroup.id}>
                <TableRow className='group/row'>
                  {headerGroup.headers.map((header) => (
                    <TableHead
                      key={header.id}
                      colSpan={header.colSpan}
                      className={cn(
                        'bg-background group-hover/row:bg-muted group-data-[state=selected]/row:bg-muted',
                        header.column.columnDef.meta?.className,
                        header.column.columnDef.meta?.thClassName
                      )}
                    >
                      {header.isPlaceholder ? null : flexRender(
                        header.column.columnDef.header,
                        header.getContext()
                      )}
                    </TableHead>
                  ))}
                </TableRow>
              </Fragment>
            ))}
          </TableHeader>
          <TableBody>
            {isLoading ? (
              <TableRow>
                <TableCell
                  colSpan={columns.length}
                  className='h-24 text-center text-muted-foreground'
                >
                  {t('Loading...')}
                </TableCell>
              </TableRow>
            ) : isError ? (
              <TableRow>
                <TableCell
                  colSpan={columns.length}
                  className='h-24 text-center text-destructive'
                >
                  {t('Failed to load audit logs.')}
                </TableCell>
              </TableRow>
            ) : table.getRowModel().rows?.length ? (
              table.getRowModel().rows.map((row) => (
                <TableRow key={row.id} className='group/row'>
                  {row.getVisibleCells().map((cell) => (
                    <TableCell
                      key={cell.id}
                      className={cn(
                        'bg-background group-hover/row:bg-muted',
                        cell.column.columnDef.meta?.className,
                        cell.column.columnDef.meta?.tdClassName
                      )}
                    >
                      {flexRender(
                        cell.column.columnDef.cell,
                        cell.getContext()
                      )}
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : (
              <TableRow>
                <TableCell
                  colSpan={columns.length}
                  className='h-24 text-center'
                >
                  {t('No results.')}
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>
      <DataTablePagination table={table} className='mt-auto' />
    </div>
  )
}
