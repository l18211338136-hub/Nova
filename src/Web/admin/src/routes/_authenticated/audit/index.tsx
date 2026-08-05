import z from 'zod'
import { createFileRoute } from '@tanstack/react-router'
import { AuditLogs } from '@/features/audit'

const auditSearchSchema = z.object({
  page: z.number().optional().catch(1),
  pageSize: z.number().optional().catch(10),
  search: z.string().optional().catch(''),
  httpMethod: z.string().optional().catch(''),
  clientIp: z.string().optional().catch(''),
  statusCode: z.coerce.number().optional().catch(undefined),
  isSlowRequest: z.boolean().optional(),
  hasSanitizedData: z.boolean().optional(),
  createdAt: z.string().optional().catch(''),
})

export const Route = createFileRoute('/_authenticated/audit/')({
  validateSearch: auditSearchSchema,
  component: AuditLogs,
})
