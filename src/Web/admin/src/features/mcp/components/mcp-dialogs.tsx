import { McpDeleteDialog } from './mcp-delete-dialog'
import { McpEditDialog } from './mcp-edit-dialog'
import { McpImportDialog } from './mcp-import-dialog'
import { McpServerDetailDialog } from './mcp-server-detail-dialog'
import { useMcpContext } from './mcp-provider'

/**
 * MCP 弹窗集中装配（范式同 `mcp-keys-dialogs.tsx`）：
 * - 依赖当前行的弹窗用 `key` 强制重挂载，保证切换行时表单默认值 / 搜索状态刷新；
 * - 关闭后延迟 500ms 清空 `currentRow`，避免关闭动画期间内容闪空。
 */
export function McpDialogs() {
  const { open, setOpen, currentRow, setCurrentRow } = useMcpContext()

  const closeAndReset = (type: 'edit-server' | 'detail-server' | 'delete-server') => (state: boolean) => {
    setOpen(state ? type : null)
    if (!state) {
      window.setTimeout(() => setCurrentRow(null), 500)
    }
  }

  return (
    <>
      <McpImportDialog />
      <McpDeleteDialog />

      {currentRow && (
        <>
          <McpServerDetailDialog
            key={`mcp-server-detail-${currentRow.id}`}
            currentRow={currentRow}
            open={open === 'detail-server'}
            onOpenChange={closeAndReset('detail-server')}
          />

          <McpEditDialog
            key={`mcp-server-edit-${currentRow.id}`}
            currentRow={currentRow}
            open={open === 'edit-server'}
            onOpenChange={closeAndReset('edit-server')}
          />
        </>
      )}
    </>
  )
}
