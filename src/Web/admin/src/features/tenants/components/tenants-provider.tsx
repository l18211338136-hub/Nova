import { createContext, useContext, useState } from 'react'
import { TenantDto as Tenant } from '@/api/model'

type TenantsContextType = {
  open: 'create' | 'update' | 'delete' | null
  setOpen: (str: 'create' | 'update' | 'delete' | null) => void
  currentRow: Tenant | null
  setCurrentRow: (row: Tenant | null) => void
}

const TenantsContext = createContext<TenantsContextType | null>(null)

export default function TenantsProvider({ children }: { children: React.ReactNode }) {
  const [open, setOpen] = useState<'create' | 'update' | 'delete' | null>(null)
  const [currentRow, setCurrentRow] = useState<Tenant | null>(null)

  return (
    <TenantsContext.Provider value={{ open, setOpen, currentRow, setCurrentRow }}>
      {children}
    </TenantsContext.Provider>
  )
}

export const useTenantsContext = () => {
  const context = useContext(TenantsContext)
  if (!context) throw new Error('useTenantsContext has to be used within <TenantsProvider>')
  return context
}
