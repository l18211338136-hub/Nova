import React from 'react'
import { type RoleDto as Role } from '@/api/model'

type RolesDialogType = 'create' | 'edit' | 'delete' | 'permissions'

interface RolesContextType {
  open: RolesDialogType | null
  setOpen: (str: RolesDialogType | null) => void
  currentRow: Role | null
  setCurrentRow: React.Dispatch<React.SetStateAction<Role | null>>
}

const RolesContext = React.createContext<RolesContextType | null>(null)

export default function RolesProvider({ children }: { children: React.ReactNode }) {
  const [open, setOpen] = React.useState<RolesDialogType | null>(null)
  const [currentRow, setCurrentRow] = React.useState<Role | null>(null)

  return (
    <RolesContext.Provider value={{ open, setOpen, currentRow, setCurrentRow }}>
      {children}
    </RolesContext.Provider>
  )
}

// eslint-disable-next-line react-refresh/only-export-components
export const useRolesContext = () => {
  const context = React.useContext(RolesContext)

  if (!context) {
    throw new Error('useRolesContext has to be used within <RolesProvider>')
  }

  return context
}
