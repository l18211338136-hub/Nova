import { useMenusContext } from './menus-provider'
import { MenusActionDialog } from './menus-action-dialog'
import { MenusDeleteDialog } from './menus-delete-dialog'

export function MenusDialogs() {
  const { open, setOpen, currentRow } = useMenusContext()

  return (
    <>
      <MenusActionDialog
        key={currentRow?.id || 'new'}
        open={open === 'create' || open === 'edit' || open === 'create-sub'}
        onOpenChange={(isOpen: boolean) => !isOpen && setOpen(null)}
        currentRow={currentRow}
        isEdit={open === 'edit'}
        isSubMenu={open === 'create-sub'}
      />
      <MenusDeleteDialog
        key={`delete-${currentRow?.id || 'new'}`}
        open={open === 'delete'}
        onOpenChange={(isOpen: boolean) => !isOpen && setOpen(null)}
        currentRow={currentRow}
      />
    </>
  )
}
