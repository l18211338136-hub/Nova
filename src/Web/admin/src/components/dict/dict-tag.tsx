import React from 'react'
import { Badge } from '@/components/ui/badge'
import { useDictionaryItemsByCode as useDictionaryItemsByCode } from '@/api/endpoints/dictionaries'

interface DictTagProps {
  code: string
  value?: string
  fallbackLabel?: string
  className?: string
}

export const DictTag: React.FC<DictTagProps> = ({
  code,
  value,
  fallbackLabel,
  className,
}) => {
  const { data } = useDictionaryItemsByCode(code)
  const items = data?.data || []
  const item = items.find((x) => x.value === value)

  if (!item) {
    return <Badge variant='outline' className={className}>{fallbackLabel || value || '-'}</Badge>
  }

  let variant: 'default' | 'secondary' | 'destructive' | 'outline' = 'default'
  if (item.tagType === 'warning') variant = 'secondary'
  else if (item.tagType === 'danger') variant = 'destructive'
  else if (item.tagType === 'info') variant = 'outline'

  return (
    <Badge variant={variant} className={className}>
      {item.label}
    </Badge>
  )
}
