import React from 'react'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { useDictionaryItemsByCode as useDictionaryItemsByCode } from '@/api/endpoints/dictionaries'
import { useTranslation } from 'react-i18next'

interface DictSelectProps {
  code: string
  value?: string
  onValueChange?: (value: string) => void
  placeholder?: string
  disabled?: boolean
  className?: string
}

export const DictSelect: React.FC<DictSelectProps> = ({
  code,
  value,
  onValueChange,
  placeholder,
  disabled,
  className,
}) => {
  const { t } = useTranslation()
  const { data, isLoading } = useDictionaryItemsByCode(code)
  const items = data?.data || []

  return (
    <Select value={value} onValueChange={onValueChange} disabled={disabled || isLoading}>
      <SelectTrigger className={className}>
        <SelectValue placeholder={placeholder || t('Please select')} />
      </SelectTrigger>
      <SelectContent>
        {items.map((item) => (
          <SelectItem key={item.id} value={item.value || ''}>
            {item.label}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  )
}
