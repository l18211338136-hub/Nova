import { createFileRoute } from '@tanstack/react-router'
import TrashBinFeature from '@/features/trash-bin'

export const Route = createFileRoute('/_authenticated/trash-bin/')({
  component: TrashBinFeature,
})
