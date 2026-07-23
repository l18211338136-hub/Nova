import { z } from 'zod'
import { createFileRoute, redirect } from '@tanstack/react-router'
import { SignIn } from '@/features/auth/sign-in'
import { useAuthStore } from '@/stores/auth-store'

const searchSchema = z.object({
  redirect: z.string().optional(),
})

export const Route = createFileRoute('/(auth)/sign-in')({
  beforeLoad: () => {
    const isAuthenticated = !!useAuthStore.getState().auth.accessToken
    if (isAuthenticated) {
      throw redirect({
        to: '/',
      })
    }
  },
  component: SignIn,
  validateSearch: searchSchema,
})
