import { useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import {
  type ColumnFiltersState,
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
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import {
  useGetTrashBinItems,
  useRestoreTrashBinItem,
  useHardDeleteTrashBinItem,
  getGetTrashBinItemsQueryKey,
} from '@/api/endpoints/trash-bin'
import type { TrashBinItemDto } from '@/api/model'
import { buildODataFilter, buildODataOrderBy } from '@/lib/odata'
import { DataTableToolbar, DataTablePagination } from '@/components/data-table'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { useTrashBinColumns } from './trash-bin-columns'
import { TrashBinActionDialog } from './trash-bin-dialog'

export function TrashBinTable() {
  const { t } = useTranslation()
  const queryClient = useQueryClient()

  // 1. 表格内部状态
  const [columnFilters, setColumnFilters] = useState<ColumnFiltersState>([])
  const [sorting, setSorting] = useState<SortingState>([])
  const [columnVisibility, setColumnVisibility] = useState<VisibilityState>({})
  const [rowSelection, setRowSelection] = useState({})
  const [pagination, setPagination] = useState({ pageIndex: 0, pageSize: 10 })

  const [actionTarget, setActionTarget] = useState<{
    item: TrashBinItemDto
    action: 'restore' | 'hardDelete'
  } | null>(null)

  // 2. 构建基于 OData 的条件与排序
  const $filter = buildODataFilter(columnFilters)
  const $orderby = buildODataOrderBy(sorting) || 'DeletedAt desc'

  // 3. 调用由 Orval 编译产生的 Hook useGetTrashBinItems
  const { data: response, isLoading } = useGetTrashBinItems({
    query: {
      queryKey: ['trash-bin-items', pagination, columnFilters, sorting],
    },
    request: {
      params: {
        $count: true,
        $top: pagination.pageSize,
        $skip: pagination.pageIndex * pagination.pageSize,
        ...($filter && { $filter }),
        ...($orderby && { $orderby }),
      },
    },
  })

  const items = (response?.data?.items as TrashBinItemDto[]) || []
  const totalCount = response?.data?.total ?? 0
  const pageCount = Math.ceil(totalCount / pagination.pageSize)

  // 4. 调用一键恢复与彻底强删的 Mutations
  const restoreMutation = useRestoreTrashBinItem({
    mutation: {
      onSuccess: (_, variables) => {
        toast.success(`${t('Data restored successfully:')} ${variables.data.entityType} (${variables.data.id})`)
        setActionTarget(null)
        // 使全局查询缓存失效，确保切回用户/角色/菜单页面时自动加载最新数据，无需手动按 F5 刷新
        queryClient.invalidateQueries()
      },
      onError: (err: any) => {
        toast.error(`${t('Failed to restore data:')} ` + (err?.message || t('Operation failed')))
      },
    },
  })

  const hardDeleteMutation = useHardDeleteTrashBinItem({
    mutation: {
      onSuccess: (_, variables) => {
        toast.success(`${t('Data hard deleted successfully:')} ${variables.data.entityType} (${variables.data.id})`)
        setActionTarget(null)
        // 使全局查询缓存失效，同步全站所有模块列表
        queryClient.invalidateQueries()
      },
      onError: (err: any) => {
        toast.error(`${t('Failed to hard delete data:')} ` + (err?.message || t('Operation failed')))
      },
    },
  })

  const handleConfirmAction = () => {
    if (!actionTarget || !actionTarget.item.id || !actionTarget.item.entityType) return
    if (actionTarget.action === 'restore') {
      restoreMutation.mutate({
        data: {
          entityType: actionTarget.item.entityType,
          id: actionTarget.item.id,
        },
      })
    } else {
      hardDeleteMutation.mutate({
        data: {
          entityType: actionTarget.item.entityType,
          id: actionTarget.item.id,
        },
      })
    }
  }

  const columns = useTrashBinColumns({
    onRestore: (item) => setActionTarget({ item, action: 'restore' }),
    onHardDelete: (item) => setActionTarget({ item, action: 'hardDelete' }),
  })

  // 5. 初始化表格实例
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
      columnFilters,
      columnVisibility,
      rowSelection,
    },
    onPaginationChange: setPagination,
    onColumnFiltersChange: setColumnFilters,
    onSortingChange: setSorting,
    onColumnVisibilityChange: setColumnVisibility,
    onRowSelectionChange: setRowSelection,
    getCoreRowModel: getCoreRowModel(),
    getFilteredRowModel: getFilteredRowModel(),
    getSortedRowModel: getSortedRowModel(),
    getFacetedRowModel: getFacetedRowModel(),
    getFacetedUniqueValues: getFacetedUniqueValues(),
  })

  const isActionLoading = restoreMutation.isPending || hardDeleteMutation.isPending

  return (
    <div className='flex flex-1 flex-col gap-4'>
      <DataTableToolbar
        table={table}
        hideSearch={true}
        filters={[]}
      />

      <div className='rounded-md border bg-card overflow-hidden'>
        <Table className='table-fixed'>
          <TableHeader>
            {table.getHeaderGroups().map((headerGroup) => (
              <TableRow key={headerGroup.id}>
                {headerGroup.headers.map((header) => (
                  <TableHead key={header.id}>
                    {header.isPlaceholder
                      ? null
                      : flexRender(header.column.columnDef.header, header.getContext())}
                  </TableHead>
                ))}
              </TableRow>
            ))}
          </TableHeader>

          <TableBody>
            {isLoading ? (
              <TableRow>
                <TableCell colSpan={columns.length} className='h-32 text-center text-muted-foreground'>
                  {t('Loading trash bin data...')}
                </TableCell>
              </TableRow>
            ) : table.getRowModel().rows?.length ? (
              table.getRowModel().rows.map((row) => (
                <TableRow key={row.id}>
                  {row.getVisibleCells().map((cell) => (
                    <TableCell key={cell.id}>
                      {flexRender(cell.column.columnDef.cell, cell.getContext())}
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : (
              <TableRow>
                <TableCell colSpan={columns.length} className='h-32 text-center text-muted-foreground'>
                  {t('No soft-deleted data in trash bin.')}
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>

      <DataTablePagination table={table} className='mt-auto' />

      <TrashBinActionDialog
        target={actionTarget}
        loading={isActionLoading}
        onClose={() => setActionTarget(null)}
        onConfirm={handleConfirmAction}
      />
    </div>
  )
}
