import { createFileRoute } from '@tanstack/react-router';
import StorageFeature from '@/features/storage';

export const Route = createFileRoute('/_authenticated/storage/')({
  component: StorageFeature,
});
