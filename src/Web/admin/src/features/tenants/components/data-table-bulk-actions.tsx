import { Table } from '@tanstack/react-table'
import { TenantDto as Tenant } from '@/api/model'

interface DataTableBulkActionsProps {
  table: Table<Tenant>
}

export function DataTableBulkActions({ table }: DataTableBulkActionsProps) {
  return null // implement bulk actions if needed
}
