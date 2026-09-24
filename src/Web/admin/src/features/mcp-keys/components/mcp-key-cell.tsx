'use client'

import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Check, Copy, Eye, EyeOff } from 'lucide-react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'

type McpKeyCellProps = {
  value?: string
}

/**
 * 密钥单元格：默认遮罩显示，小眼睛可切换明文；右侧复制按钮直接把明文写入剪贴板。
 * 明文超长时自动换行（break-all），按钮固定不收缩，避免遮挡。
 */
export function McpKeyCell({ value }: McpKeyCellProps) {
  const { t } = useTranslation()
  const [revealed, setRevealed] = useState(false)
  const [copied, setCopied] = useState(false)

  const masked = useMemo(() => {
    if (!value) return ''
    if (value.length <= 12) return value
    return `${value.slice(0, 4)}••••••••${value.slice(-4)}`
  }, [value])

  const handleCopy = async () => {
    if (!value) return
    try {
      await navigator.clipboard.writeText(value)
      setCopied(true)
      toast.success(t('Copied'))
      window.setTimeout(() => setCopied(false), 2000)
    } catch {
      toast.error(t('Copy failed, please copy manually'))
    }
  }

  return (
    <div className='flex w-full min-w-0 items-center gap-1.5'>
      <div
        className={cn(
          'min-w-0 flex-1 font-mono text-xs',
          revealed ? 'break-all' : 'truncate'
        )}
        title={revealed ? undefined : value}
      >
        {revealed ? value : masked}
      </div>
      <Button
        type='button'
        variant='ghost'
        size='icon'
        className='h-7 w-7 shrink-0'
        onClick={() => setRevealed((v) => !v)}
        title={t('Show or hide the key')}
      >
        {revealed ? <EyeOff size={15} /> : <Eye size={15} />}
      </Button>
      <Button
        type='button'
        variant='ghost'
        size='icon'
        className='h-7 w-7 shrink-0'
        onClick={handleCopy}
        title={t('Copy Key')}
      >
        {copied ? (
          <Check size={15} className='text-green-600' />
        ) : (
          <Copy size={15} />
        )}
      </Button>
    </div>
  )
}
