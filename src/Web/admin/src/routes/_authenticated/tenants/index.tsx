import { createFileRoute } from '@tanstack/react-router'
import Tenants from '@/features/tenants'

export const Route = createFileRoute('/_authenticated/tenants/')({
  validateSearch: (search) => search as Record<string, unknown>,
  component: () => {
    const search = Route.useSearch()
    const navigate = Route.useNavigate()
    return <Tenants search={search} navigate={navigate} />
  },
})
