import { createFileRoute } from '@tanstack/react-router'
import { z } from 'zod'
import Roles from '@/features/roles'

// Define the schema for URL search parameters (like users)
const rolesSearchSchema = z.object({
  page: z.number().optional().catch(1),
  pageSize: z.number().optional().catch(10),
  name: z.string().optional(),
  displayName: z.string().optional(),
  isEnabled: z.boolean().optional(),
})

export const Route = createFileRoute('/_authenticated/roles/')({
  component: RolesRoute,
  validateSearch: rolesSearchSchema,
})

function RolesRoute() {
  const search = Route.useSearch()
  const navigate = Route.useNavigate()
  
  return <Roles search={search} navigate={navigate} />
}
