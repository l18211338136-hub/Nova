import {
  ArrowDownIcon,
  ArrowUpIcon,
  CaretSortIcon,
  EyeNoneIcon,
  MixerHorizontalIcon,
} from '@radix-ui/react-icons'
import { useTranslation } from 'react-i18next'
import { type Column } from '@tanstack/react-table'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { Separator } from '@/components/ui/separator'
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover'
import { DataTableColumnFilter } from './column-filter'

type DataTableColumnHeaderProps<TData, TValue> =
  React.HTMLAttributes<HTMLDivElement> & {
    column: Column<TData, TValue>
    title: string
  }

export function DataTableColumnHeader<TData, TValue>({
  column,
  title,
  className,
}: DataTableColumnHeaderProps<TData, TValue>) {
  const { t } = useTranslation()

  if (!column.getCanSort()) {
    return <div className={cn(className)}>{t(title)}</div>
  }

  const hasFilter = column.getCanFilter() && (column.columnDef.meta as any)?.filterType
  const filterValue = column.getFilterValue()
  const isFiltered = (() => {
    if (filterValue === undefined || filterValue === null || filterValue === '') return false
    // number/date 筛选值是对象 { op, from?, to? }，只有 op 没填数值不算已筛选
    if (typeof filterValue === 'object') {
      const obj = filterValue as Record<string, unknown>
      return obj.from !== undefined || obj.to !== undefined
    }
    return true
  })()

  return (
    <div className={cn('flex items-center space-x-2', className)}>
      <Popover>
        <PopoverTrigger asChild>
          <Button
            variant='ghost'
            size='sm'
            className='h-8 data-[state=open]:bg-accent'
          >
            <span>{t(title)}</span>
            {column.getIsSorted() === 'desc' ? (
              <ArrowDownIcon className='ms-2 h-4 w-4' />
            ) : column.getIsSorted() === 'asc' ? (
              <ArrowUpIcon className='ms-2 h-4 w-4' />
            ) : (
              <CaretSortIcon className='ms-2 h-4 w-4' />
            )}
            {hasFilter && isFiltered && (
              <MixerHorizontalIcon className='ms-1.5 h-3.5 w-3.5 text-primary' />
            )}
          </Button>
        </PopoverTrigger>
        <PopoverContent align='start' className='w-64 p-3'>
          <div className='space-y-2'>
            <div className='flex flex-col gap-1'>
              <Button
                variant='ghost'
                size='sm'
                className='justify-start'
                onClick={() => column.toggleSorting(false)}
              >
                <ArrowUpIcon className='me-2 size-3.5 text-muted-foreground/70' />
                {t('Asc')}
              </Button>
              <Button
                variant='ghost'
                size='sm'
                className='justify-start'
                onClick={() => column.toggleSorting(true)}
              >
                <ArrowDownIcon className='me-2 size-3.5 text-muted-foreground/70' />
                {t('Desc')}
              </Button>
              {column.getCanHide() && (
                <>
                  <Separator className='my-1' />
                  <Button
                    variant='ghost'
                    size='sm'
                    className='justify-start'
                    onClick={() => column.toggleVisibility(false)}
                  >
                    <EyeNoneIcon className='me-2 size-3.5 text-muted-foreground/70' />
                    {t('Hide')}
                  </Button>
                </>
              )}
              {hasFilter && (
                <>
                  <Separator className='my-1' />
                  <div className='pt-1'>
                    <div className='text-xs text-muted-foreground mb-1.5'>
                      {t('Filter')}
                    </div>
                    <DataTableColumnFilter column={column} />
                  </div>
                </>
              )}
            </div>
          </div>
        </PopoverContent>
      </Popover>
    </div>
  )
}
