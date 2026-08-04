import { createFileRoute } from '@tanstack/react-router'
import { AuditLogs } from '@/features/audit'

export const Route = createFileRoute('/_authenticated/audit/')({
  component: AuditLogs,
})
