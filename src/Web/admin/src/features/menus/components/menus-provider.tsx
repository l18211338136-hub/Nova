import React from 'react'

import type { MenuDto as ApiMenuDto } from '@/api/model'

export type MenuDto = ApiMenuDto & {
  subRows?: MenuDto[]
}

type MenusDialogType = 'create' | 'edit' | 'delete' | 'create-sub'

interface MenusContextType {
  open: MenusDialogType | null
  setOpen: (str: MenusDialogType | null) => void
  currentRow: MenuDto | null
  setCurrentRow: React.Dispatch<React.SetStateAction<MenuDto | null>>
}

const MenusContext = React.createContext<MenusContextType | null>(null)

export default function MenusProvider({ children }: { children: React.ReactNode }) {
  const [open, setOpen] = React.useState<MenusDialogType | null>(null)
  const [currentRow, setCurrentRow] = React.useState<MenuDto | null>(null)

  return (
    <MenusContext.Provider value={{ open, setOpen, currentRow, setCurrentRow }}>
      {children}
    </MenusContext.Provider>
  )
}

// eslint-disable-next-line react-refresh/only-export-components
export const useMenusContext = () => {
  const context = React.useContext(MenusContext)

  if (!context) {
    throw new Error('useMenusContext has to be used within <MenusProvider>')
  }

  return context
}
