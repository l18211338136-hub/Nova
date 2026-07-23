import { createFileRoute } from '@tanstack/react-router'
import { ResetPassword } from '@/features/auth/reset-password'
import { z } from 'zod'

const searchSchema = z.object({
  email: z.string().optional().catch(''),
})

export const Route = createFileRoute('/(auth)/reset-password')({
  component: ResetPassword,
  validateSearch: searchSchema,
})
