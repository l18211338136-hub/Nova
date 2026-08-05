import { useEffect, useState, useCallback, Fragment } from 'react'
import { keepPreviousData } from '@tanstack/react-query'
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
import { Button } from '@/components/ui/button'
import { Cross2Icon } from '@radix-ui/react-icons'
import { DataTablePagination } from '@/components/data-table'
import { DataTableViewOptions } from '@/components/data-table/view-options'
import { type OperationLogDto } from '@/api/model'
import { useGetOperationLogs } from '@/api/endpoints/audit'
import { useOperationColumns } from './operation-columns'
import { OperationLogDetailDialog } from './operation-log-detail-dialog'
import { type DateFilterValue } from '@/components/date-range-picker'

type DataTableProps = {
  search: Record<string, unknown>
  navigate: NavigateFn
}

export function OperationTable({ search, navigate }: DataTableProps) {
  const { t } = useTranslation()
  const [selectedLog, setSelectedLog] = useState<OperationLogDto | null>(null)
  const [detailOpen, setDetailOpen] = useState(false)

  const handleViewDetail = useCallback((log: OperationLogDto) => {
    setSelectedLog(log)
    setDetailOpen(true)
  }, [])

  const columns = useOperationColumns({ onViewDetail: handleViewDetail })
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
      { columnId: 'httpMethod', searchKey: 'httpMethod', type: 'string' },
      { columnId: 'statusCode', searchKey: 'statusCode', type: 'string' },
      { columnId: 'clientIp', searchKey: 'clientIp', type: 'string' },
      { columnId: 'isSlowRequest', searchKey: 'isSlowRequest', type: 'boolean' },
      { columnId: 'hasSanitizedData', searchKey: 'hasSanitizedData', type: 'boolean' },
      {
        columnId: 'createdAt',
        searchKey: 'createdAt',
        type: 'string' as const,
        serialize: (v: unknown) => {
          if (!v || typeof v !== 'object') return ''
          const d = v as DateFilterValue
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

  // 从 columnFilters 提取各筛选条件
  const getFilterValue = (columnId: string) => {
    const filter = columnFilters.find((f) => f.id === columnId)
    return filter ? (filter.value as any) : undefined
  }

  const httpMethod = getFilterValue('httpMethod')
  const isSlowRequest = getFilterValue('isSlowRequest')
  const hasSanitizedData = getFilterValue('hasSanitizedData')
  const statusCodeRaw = getFilterValue('statusCode')
  const statusCode = statusCodeRaw !== undefined && statusCodeRaw !== '' ? Number(statusCodeRaw) : undefined
  const clientIp = getFilterValue('clientIp')
  const createdAtVal = getFilterValue('createdAt') as DateFilterValue | undefined
  const startDate = createdAtVal?.from
  const endDate = createdAtVal?.to

  const isFiltered = columnFilters.length > 0

  const { data: apiResponse, isLoading, isError } = useGetOperationLogs(
    {
      page: pagination.pageIndex + 1,
      pageSize: pagination.pageSize,
      httpMethod: httpMethod,
      statusCode: statusCode && !isNaN(statusCode) ? statusCode : undefined,
      isSlowRequest: isSlowRequest,
      hasSanitizedData: hasSanitizedData,
      search: clientIp || undefined,
      startDate: startDate || undefined,
      endDate: endDate || undefined,
    },
    {
      query: {
        queryKey: ['operation-logs', pagination, columnFilters, sorting],
        placeholderData: keepPreviousData,
      },
    }
  )

  const items = (apiResponse?.data?.items as OperationLogDto[]) ?? []
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

  const handleReset = () => {
    table.resetColumnFilters()
  }

  return (
    <div
      className={cn(
        'max-sm:has-[div[role="toolbar"]]:mb-16',
        'flex flex-1 flex-col gap-4'
      )}
    >
      {/* Toolbar：重置 + 列可见性 */}
      <div className='flex items-center justify-between'>
        <div className='flex flex-1 items-center space-x-2'>
          {isFiltered && (
            <Button
              variant='ghost'
              onClick={handleReset}
              className='h-8 px-2 lg:px-3'
            >
              {t('Reset')}
              <Cross2Icon className='ms-2 h-4 w-4' />
            </Button>
          )}
        </div>
        <DataTableViewOptions table={table} />
      </div>

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
                  {t('加载操作日志失败。')}
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
                        (cell.column.columnDef.meta as any)?.tdClassName
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
                  {t('暂无日志记录。')}
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>

      <DataTablePagination table={table} className='mt-auto' />

      <OperationLogDetailDialog
        open={detailOpen}
        onOpenChange={setDetailOpen}
        log={selectedLog}
      />
    </div>
  )
}
