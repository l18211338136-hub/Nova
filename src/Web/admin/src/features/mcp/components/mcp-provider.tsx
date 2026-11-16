import React, { useState } from 'react'
import useDialogState from '@/hooks/use-dialog-state'
import { type McpServerDto, type McpToolDto } from '../types'

type McpDialogType = 'import' | 'delete-server' | 'delete-tool' | null

type McpRow = McpServerDto | McpToolDto

type McpContextType = {
  open: McpDialogType
  setOpen: (str: McpDialogType) => void
  currentRow: McpRow | null
  setCurrentRow: React.Dispatch<React.SetStateAction<McpRow | null>>
}

const McpContext = React.createContext<McpContextType | null>(null)

export function McpProvider({ children }: { children: React.ReactNode }) {
  const [open, setOpen] = useDialogState<McpDialogType>(null)
  const [currentRow, setCurrentRow] = useState<McpRow | null>(null)

  return (
    <McpContext
      value={{ open, setOpen, currentRow, setCurrentRow }}
    >
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
