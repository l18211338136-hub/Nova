import { Table } from '@tanstack/react-table'
import { useTranslation } from 'react-i18next'
import { Trash2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { type RoleDto as Role } from '@/api/model'

interface DataTableBulkActionsProps {
  table: Table<Role>
}

export function DataTableBulkActions({ table }: DataTableBulkActionsProps) {
  const { t } = useTranslation()
  const selectedRows = table.getFilteredSelectedRowModel().rows

  if (selectedRows.length === 0) return null

  return (
    <div className='fixed bottom-4 left-1/2 -translate-x-1/2 flex items-center gap-4 rounded-full bg-background px-6 py-3 shadow-lg border'>
      <span className='text-sm text-muted-foreground'>
        {selectedRows.length} {t('selected')}
      </span>
      {/* 
        You can add multi-delete functionality here by creating a multi-delete mutation
        or calling delete one by one. Left as UI placeholder since no backend multi-delete yet.
      */}
      <Button variant='destructive' size='sm' className='rounded-full'>
        <Trash2 className='mr-2 h-4 w-4' />
        {t('Delete')}
      </Button>
    </div>
  )
}
