import React, { useState } from 'react'
import useDialogState from '@/hooks/use-dialog-state'
import { type McpKeyDto } from '@/api/model'

type McpKeysDialogType = 'create' | 'edit' | 'delete' | null

type McpKeysContextType = {
  open: McpKeysDialogType
  setOpen: (str: McpKeysDialogType) => void
  currentRow: McpKeyDto | null
  setCurrentRow: React.Dispatch<React.SetStateAction<McpKeyDto | null>>
}

const McpKeysContext = React.createContext<McpKeysContextType | null>(null)

export function McpKeysProvider({ children }: { children: React.ReactNode }) {
  const [open, setOpen] = useDialogState<McpKeysDialogType>(null)
  const [currentRow, setCurrentRow] = useState<McpKeyDto | null>(null)

  return (
    <McpKeysContext
      value={{ open, setOpen, currentRow, setCurrentRow }}
    >
      {children}
    </McpKeysContext>
  )
}

// eslint-disable-next-line react-refresh/only-export-components
export const useMcpKeysContext = () => {
  const mcpKeysContext = React.useContext(McpKeysContext)

  if (!mcpKeysContext) {
    throw new Error('useMcpKeysContext has to be used within <McpKeysProvider>')
  }

  return mcpKeysContext
}
