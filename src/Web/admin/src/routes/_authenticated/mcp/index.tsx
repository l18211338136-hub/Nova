import { z } from 'zod'
import { createFileRoute } from '@tanstack/react-router'
import { Mcp } from '@/features/mcp'

const mcpSearchSchema = z.object({
  page: z.number().optional().catch(1),
  pageSize: z.number().optional().catch(10),
  // 服务列表筛选（工具列表已随 tab 下线，「详情」用弹窗内本地搜索）
  serverName: z.string().optional().catch(''),
  serverBaseUrl: z.string().optional().catch(''),
})

export const Route = createFileRoute('/_authenticated/mcp/')({
  validateSearch: mcpSearchSchema,
  component: Mcp,
})
