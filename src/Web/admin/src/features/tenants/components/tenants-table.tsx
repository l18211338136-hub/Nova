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
import { type TenantDto as Tenant } from '@/api/model'
import { useTenants } from '@/api/endpoints/tenants'
import { useTenantsColumns } from './tenants-columns'
import { DataTableBulkActions } from './data-table-bulk-actions'
import { buildODataFilter, buildODataOrderBy } from '@/lib/odata'

type DataTableProps = {
  search: Record<string, unknown>
  navigate: NavigateFn
}

export function TenantsTable({ search, navigate }: DataTableProps) {
  const { t } = useTranslation()
  const columns = useTenantsColumns()
  // Local UI-only states
  const [rowSelection, setRowSelection] = useState({})
  const [columnVisibility, setColumnVisibility] = useState<VisibilityState>({})
  const [sorting, setSorting] = useState<SortingState>([])

  // Synced with URL states
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
      { columnId: 'id', searchKey: 'id', type: 'string' },
      { columnId: 'name', searchKey: 'name', type: 'string' },
      { columnId: 'identifier', searchKey: 'identifier', type: 'string' },
      { columnId: 'adminEmail', searchKey: 'adminEmail', type: 'string' },
      { columnId: 'isActive', searchKey: 'isActive', type: 'boolean' },
      { columnId: 'validUpto', searchKey: 'validUpto', type: 'string' },
    ],
  })

  // Build OData params using global utility
  const $filter = buildODataFilter(columnFilters)
  const $orderby = buildODataOrderBy(sorting) || 'Id asc'

  // Fetch from OData API
  const { data: apiResponse } = useTenants({
    query: {
      queryKey: ['tenants', pagination, columnFilters, sorting],
    },
    request: {
      params: {
        $count: true,
        $skip: pagination.pageIndex * pagination.pageSize,
        $top: pagination.pageSize,
        ...($filter && { $filter }),
        ...($orderby && { $orderby })
      }
    }
  })

  const tenantsData = (apiResponse?.data?.items as Tenant[]) ?? []
  const totalCount = apiResponse?.data?.total ?? 0
  const pageCount = Math.ceil(totalCount / pagination.pageSize)

  // eslint-disable-next-line react-hooks/incompatible-library
  const table = useReactTable({
    data: tenantsData,
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
    enableRowSelection: true,
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
      <DataTableToolbar
        table={table}
        hideSearch={true}
        filters={[]}
      />
      <div className='overflow-hidden rounded-md border'>
        <Table className="table-fixed">
          <TableHeader>
            {table.getHeaderGroups().map((headerGroup) => (
              <Fragment key={headerGroup.id}>
                <TableRow className='group/row'>
                  {headerGroup.headers.map((header) => {
                    return (
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
                    )
                  })}
                </TableRow>
              </Fragment>
            ))}
          </TableHeader>
          <TableBody>
            {table.getRowModel().rows?.length ? (
              table.getRowModel().rows.map((row) => (
                <TableRow
                  key={row.id}
                  data-state={row.getIsSelected() && 'selected'}
                  className='group/row'
                >
                  {row.getVisibleCells().map((cell) => (
                    <TableCell
                      key={cell.id}
                      className={cn(
                        'bg-background group-hover/row:bg-muted group-data-[state=selected]/row:bg-muted',
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
      <DataTableBulkActions table={table} />
    </div>
  )
}
