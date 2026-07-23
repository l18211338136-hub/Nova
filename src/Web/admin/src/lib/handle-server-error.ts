import { AxiosError } from 'axios'
import { toast } from 'sonner'
import i18n from '@/lib/i18n'

export function handleServerError(error: unknown) {
  if (import.meta.env.DEV) {
    // eslint-disable-next-line no-console
    console.log(error)
  }

  let errMsg = i18n.t('Something went wrong!')

  if (
    error &&
    typeof error === 'object' &&
    'status' in error &&
    Number(error.status) === 204
  ) {
    errMsg = i18n.t('No content.')
  }

  if (error instanceof AxiosError) {
    const data = error.response?.data
    const title = data?.title || data?.message || data?.detail
    if (typeof title === 'string' && title.length > 0) {
      errMsg = title
    }
  }

  toast.error(errMsg)
}
