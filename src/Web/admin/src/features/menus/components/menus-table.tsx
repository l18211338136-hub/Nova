import { useEffect, useState, Fragment, useMemo } from 'react'
import {
  ColumnDef,
  ColumnFiltersState,
  SortingState,
  VisibilityState,
  flexRender,
  getCoreRowModel,
  getExpandedRowModel,
  getFilteredRowModel,
  getSortedRowModel,
  useReactTable,
} from '@tanstack/react-table'
import { cn } from '@/lib/utils'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { DataTableToolbar, DataTableColumnFilter } from '@/components/data-table'
import { useTranslation } from 'react-i18next'
import { useMenusColumns } from './menus-columns'
import { MenuDto } from './menus-provider'
import { useMenus } from '@/api/endpoints/menus'



interface MenusTableProps {
  search: Record<string, unknown>
  navigate: any
}

export function MenusTable({ search: _search, navigate: _navigate }: MenusTableProps) {
  const { t } = useTranslation()
  const [rowSelection, setRowSelection] = useState({})
  const [columnVisibility, setColumnVisibility] = useState<VisibilityState>({})
  const [columnFilters, setColumnFilters] = useState<ColumnFiltersState>([])
  const [sorting, setSorting] = useState<SortingState>([])
  const [expanded, setExpanded] = useState({})

  const { data: apiResponse } = useMenus({
    request: {
      params: {
        $count: true,
        $top: 1000,
        $orderby: 'Sort asc, CreatedAt asc'
      }
    }
  })

  const treeData = useMemo(() => {
    const rawMenus = (apiResponse?.data?.items || []) as MenuDto[]
    
    // Convert flat list to tree
    const map = new Map<string, MenuDto>()
    const roots: MenuDto[] = []
    
    rawMenus.forEach(item => {
      if (item.id) {
        map.set(item.id, { ...item, subRows: [] })
      }
    })
    
    rawMenus.forEach(item => {
      if (!item.id) return
      const node = map.get(item.id)!
      if (item.parentId && map.has(item.parentId)) {
        map.get(item.parentId)!.subRows!.push(node)
      } else {
        roots.push(node)
      }
    })
    
    const sortNodes = (nodes: MenuDto[]) => {
      nodes.sort((a, b) => {
        if ((a.sort || 0) !== (b.sort || 0)) {
          return (a.sort || 0) - (b.sort || 0)
        }
        return new Date(a.createdAt || 0).getTime() - new Date(b.createdAt || 0).getTime()
      })
      nodes.forEach(n => {
        if (n.subRows && n.subRows.length > 0) sortNodes(n.subRows)
      })
    }
    sortNodes(roots)

    return roots
  }, [apiResponse?.data?.items])

  const columns = useMenusColumns()

  const table = useReactTable({
    data: treeData,
    columns,
    state: {
      sorting,
      columnVisibility,
      rowSelection,
      columnFilters,
      expanded,
    },
    enableRowSelection: true,
    onRowSelectionChange: setRowSelection,
    onSortingChange: setSorting,
    onColumnFiltersChange: setColumnFilters,
    onColumnVisibilityChange: setColumnVisibility,
    onExpandedChange: setExpanded,
    getCoreRowModel: getCoreRowModel(),
    getFilteredRowModel: getFilteredRowModel(),
    getSortedRowModel: getSortedRowModel(),
    getExpandedRowModel: getExpandedRowModel(),
    getSubRows: row => row.subRows,
    filterFromLeafRows: true, // 允许子节点匹配时保留父节点
  })

  return (
    <div
      className={cn(
        'max-sm:has-[div[role="toolbar"]]:mb-16',
        'flex flex-1 flex-col gap-4'
      )}
    >
      <DataTableToolbar table={table} hideSearch={true} filters={[]} />
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
                        {header.isPlaceholder
                          ? null
                          : flexRender(
                            header.column.columnDef.header,
                            header.getContext()
                          )}
                      </TableHead>
                    )
                  })}
                </TableRow>
                {/* 过滤器专属行 */}
                <TableRow className='group/row border-b shadow-sm'>
                  {headerGroup.headers.map((header) => {
                    return (
                      <TableHead
                        key={`${header.id}-filter`}
                        colSpan={header.colSpan}
                        className={cn(
                          'bg-muted/30 group-hover/row:bg-muted/50 py-1 align-top',
                          header.column.columnDef.meta?.className
                        )}
                      >
                        {header.isPlaceholder ? null : (
                          header.column.getCanFilter() ? (
                            <DataTableColumnFilter column={header.column} />
                          ) : null
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
    </div>
  )
}
