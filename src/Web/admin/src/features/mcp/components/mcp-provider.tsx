import React, { useState } from 'react'
import useDialogState from '@/hooks/use-dialog-state'
import { type McpServerDto } from '../types'

/**
 * MCP 服务的弹窗类型。
 *
 * 工具相关弹窗已随「MCP 工具」tab 下线（详情以只读分组视图呈现），
 * 因此不再有 `delete-tool`。
 */
type McpDialogType = 'import' | 'edit-server' | 'detail-server' | 'delete-server' | null

type McpContextType = {
  open: McpDialogType
  setOpen: (str: McpDialogType) => void
  currentRow: McpServerDto | null
  setCurrentRow: React.Dispatch<React.SetStateAction<McpServerDto | null>>
}

const McpContext = React.createContext<McpContextType | null>(null)

export function McpProvider({ children }: { children: React.ReactNode }) {
  const [open, setOpen] = useDialogState<McpDialogType>(null)
  const [currentRow, setCurrentRow] = useState<McpServerDto | null>(null)

  return (
    <McpContext value={{ open, setOpen, currentRow, setCurrentRow }}>
      {children}
    </McpContext>
  )
}

// eslint-disable-next-line react-refresh/only-export-components
export const useMcpContext = () => {
  const mcpContext = React.useContext(McpContext)

  if (!mcpContext) {
    throw new Error('useMcpContext has to be used within <McpProvider>')
  }

  return mcpContext
}
