import { McpKeysCreateDialog } from './mcp-keys-create-dialog'
import { McpKeysEditDialog } from './mcp-keys-edit-dialog'
import { McpKeysDeleteDialog } from './mcp-keys-delete-dialog'
import { useMcpKeysContext } from './mcp-keys-provider'

export function McpKeysDialogs() {
  const { open, setOpen, currentRow, setCurrentRow } = useMcpKeysContext()
  return (
    <>
      <McpKeysCreateDialog
        open={open === 'create'}
        onOpenChange={(b) => setOpen(b ? 'create' : null)}
      />

      {currentRow && (
        <>
          <McpKeysEditDialog
            key={`mcp-key-edit-${currentRow.id}`}
            currentRow={currentRow}
            open={open === 'edit'}
            onOpenChange={(b) => {
              setOpen(b ? 'edit' : null)
              if (!b) {
                window.setTimeout(() => setCurrentRow(null), 500)
              }
            }}
          />

          <McpKeysDeleteDialog
            key={`mcp-key-delete-${currentRow.id}`}
            currentRow={currentRow}
            open={open === 'delete'}
            onOpenChange={(b) => {
              setOpen(b ? 'delete' : null)
              if (!b) {
                window.setTimeout(() => setCurrentRow(null), 500)
              }
            }}
          />
        </>
      )}
    </>
  )
}
