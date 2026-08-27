import { createFileRoute } from '@tanstack/react-router'
import OrganizationsFeature from '@/features/organizations'

export const Route = createFileRoute('/_authenticated/organizations/')({
  component: OrganizationsFeature,
})
