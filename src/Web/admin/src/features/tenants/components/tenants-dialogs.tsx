import { useTenantsContext } from './tenants-provider'
import { TenantsActionDialog } from './tenants-action-dialog'
import { TenantsDeleteDialog } from './tenants-delete-dialog'

export function TenantsDialogs() {
  const { open, setOpen, currentRow, setCurrentRow } = useTenantsContext()

  return (
    <>
      <TenantsActionDialog
        key={`tenant-action-${open === 'create' ? 'create' : currentRow?.id ?? 'update'}`}
        open={open === 'create' || open === 'update'}
        onOpenChange={() => {
          setOpen(null)
          setTimeout(() => {
            setCurrentRow(null)
          }, 500)
        }}
        currentRow={open === 'update' ? currentRow : null}
      />

      <TenantsDeleteDialog
        key={`tenant-delete-${currentRow?.id}`}
        open={open === 'delete'}
        onOpenChange={() => {
          setOpen(null)
          setTimeout(() => {
            setCurrentRow(null)
          }, 500)
        }}
        currentRow={currentRow}
      />
    </>
  )
}
