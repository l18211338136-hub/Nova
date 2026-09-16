import React, { useState } from 'react'
import useDialogState from '@/hooks/use-dialog-state'

type McpDialogType = 'import' | null

type McpContextType = {
  open: McpDialogType
  setOpen: (str: McpDialogType) => void
}

const McpContext = React.createContext<McpContextType | null>(null)

export function McpProvider({ children }: { children: React.ReactNode }) {
  const [open, setOpen] = useDialogState<McpDialogType>(null)

  return (
    <McpContext value={{ open, setOpen }}>{children}</McpContext>
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
