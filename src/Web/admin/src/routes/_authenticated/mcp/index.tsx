import { z } from 'zod'
import { createFileRoute } from '@tanstack/react-router'
import { Mcp } from '@/features/mcp'

const mcpSearchSchema = z.object({
  page: z.number().optional().catch(1),
  pageSize: z.number().optional().catch(10),
  // Servers tab filters
  serverName: z.string().optional().catch(''),
  serverBaseUrl: z.string().optional().catch(''),
  // Tools tab filters
  toolName: z.string().optional().catch(''),
  toolEnabled: z.string().optional().catch(''),
})

export const Route = createFileRoute('/_authenticated/mcp/')({
  validateSearch: mcpSearchSchema,
  component: Mcp,
})
