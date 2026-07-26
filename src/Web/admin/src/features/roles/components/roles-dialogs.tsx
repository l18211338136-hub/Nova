import { RolesActionDialog } from './roles-action-dialog'
import { RolesDeleteDialog } from './roles-delete-dialog'
import { RolesPermissionsDialog } from './roles-permissions-dialog'
import { useRolesContext } from './roles-provider'

export function RolesDialogs() {
  const { open, setOpen, currentRow, setCurrentRow } = useRolesContext()
  return (
    <>
      <RolesActionDialog
        key='role-create'
        open={open === 'create'}
        onOpenChange={(b) => {
          setOpen(b ? 'create' : null)
        }}
      />

      {currentRow && (
        <>
          <RolesActionDialog
            key={`role-edit-${currentRow.id}`}
            open={open === 'edit'}
            onOpenChange={(b) => {
              setOpen(b ? 'edit' : null)
              if (!b) {
                setTimeout(() => {
                  setCurrentRow(null)
                }, 500)
              }
            }}
            currentRow={currentRow}
          />

          <RolesDeleteDialog
            key={`role-delete-${currentRow.id}`}
            open={open === 'delete'}
            onOpenChange={(b) => {
              setOpen(b ? 'delete' : null)
              if (!b) {
                setTimeout(() => {
                  setCurrentRow(null)
                }, 500)
              }
            }}
            currentRow={currentRow}
          />
          
          <RolesPermissionsDialog
            key={`role-permissions-${currentRow.id}`}
            open={open === 'permissions'}
            onOpenChange={(b) => {
              setOpen(b ? 'permissions' : null)
              if (!b) {
                setTimeout(() => {
                  setCurrentRow(null)
                }, 500)
              }
            }}
            currentRow={currentRow}
          />
        </>
      )}
    </>
  )
}
