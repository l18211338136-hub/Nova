import { createFileRoute, redirect } from '@tanstack/react-router'
import { SignUp } from '@/features/auth/sign-up'
import { useAuthStore } from '@/stores/auth-store'

export const Route = createFileRoute('/(auth)/sign-up')({
  beforeLoad: () => {
    const isAuthenticated = !!useAuthStore.getState().auth.accessToken
    if (isAuthenticated) {
      throw redirect({
        to: '/',
      })
    }
  },
  component: SignUp,
})
