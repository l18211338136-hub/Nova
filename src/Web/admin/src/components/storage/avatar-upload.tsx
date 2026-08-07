import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Upload, Loader2, Camera } from 'lucide-react'
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar'
import { Button } from '@/components/ui/button'
import { useUpload, useBindAttachment } from '@/api/endpoints/storage'

interface AvatarUploadProps {
  targetId: string
  targetType?: string
  value?: string
  onChange?: (url: string) => void
  disabled?: boolean
}

export function AvatarUpload({
  targetId,
  targetType = 'User',
  value,
  onChange,
  disabled = false,
}: AvatarUploadProps) {
  const { t } = useTranslation()
  const [uploading, setUploading] = useState(false)
  const [previewUrl, setPreviewUrl] = useState(value)

  const uploadMutation = useUpload()
  const bindAttachmentMutation = useBindAttachment()

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return

    try {
      setUploading(true)
      const localUrl = URL.createObjectURL(file)
      setPreviewUrl(localUrl)

      const uploadRes = await uploadMutation.mutateAsync({
        data: { file },
      })

      const fileObj = uploadRes.data
      if (!fileObj) return

      const realAccessUrl = fileObj.accessUrl || localUrl

      if (targetId) {
        await bindAttachmentMutation.mutateAsync({
          data: {
            fileId: fileObj.id,
            targetType,
            targetId,
            attachmentType: 1, // Avatar = 1
          },
        })
      }

      if (onChange) {
        onChange(realAccessUrl)
      }
    } catch {
      // 报错提示
    } finally {
      setUploading(false)
    }
  }

  return (
    <div className='flex items-center gap-4'>
      <div className='relative group'>
        <Avatar className='h-20 w-20 border-2 border-border shadow-sm'>
          <AvatarImage src={previewUrl || value} alt='Avatar' />
          <AvatarFallback className='bg-primary/10 text-primary font-bold text-lg'>
            {targetId ? targetId.substring(0, 2).toUpperCase() : 'U'}
          </AvatarFallback>
        </Avatar>

        <label
          htmlFor={`avatar-input-${targetId}`}
          className={`absolute inset-0 rounded-full bg-black/40 flex items-center justify-center text-white opacity-0 group-hover:opacity-100 transition-opacity cursor-pointer ${
            disabled || uploading ? 'pointer-events-none' : ''
          }`}
        >
          {uploading ? (
            <Loader2 className='h-5 w-5 animate-spin' />
          ) : (
            <Camera className='h-5 w-5' />
          )}
        </label>

        <input
          id={`avatar-input-${targetId}`}
          type='file'
          accept='image/*'
          className='hidden'
          onChange={handleFileChange}
          disabled={disabled || uploading}
        />
      </div>

      <div className='space-y-1 text-xs'>
        <Button
          type='button'
          variant='outline'
          size='sm'
          className='h-8 gap-1.5'
          onClick={() => document.getElementById(`avatar-input-${targetId}`)?.click()}
          disabled={disabled || uploading}
        >
          <Upload className='h-3.5 w-3.5' />
          {uploading ? t('Uploading...') : t('Change Avatar')}
        </Button>
        <p className='text-[11px] text-muted-foreground'>
          {t('Supports JPG, PNG, WEBP formats, max size 5MB.')}
        </p>
      </div>
    </div>
  )
}
