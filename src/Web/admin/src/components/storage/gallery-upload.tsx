import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Plus, X, Image as ImageIcon, Loader2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { useUpload, useBindAttachment } from '@/api/endpoints/storage'

interface GalleryItem {
  id: string
  url: string
  name: string
  sort: number
}

interface GalleryUploadProps {
  targetType: string
  targetId: string
  items?: GalleryItem[]
  maxCount?: number
  onChange?: (items: GalleryItem[]) => void
}

const generateUniqueId = () => {
  return typeof crypto !== 'undefined' && crypto.randomUUID
    ? crypto.randomUUID()
    : Math.random().toString(36).substring(2, 9)
}

export function GalleryUpload({
  targetType,
  targetId,
  items = [],
  maxCount = 9,
  onChange,
}: GalleryUploadProps) {
  const { t } = useTranslation()
  const [gallery, setGallery] = useState<GalleryItem[]>(items)
  const [uploading, setUploading] = useState(false)

  const uploadMutation = useUpload()
  const bindAttachmentMutation = useBindAttachment()

  const handleAddFiles = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(e.target.files || [])
    if (files.length === 0) return

    try {
      setUploading(true)
      const newItems: GalleryItem[] = []

      for (let i = 0; i < files.length; i++) {
        const file = files[i]
        const uploadRes = await uploadMutation.mutateAsync({
          data: { file },
        })

        const fileObj = uploadRes.data
        if (!fileObj) continue

        const sortIndex = gallery.length + i

        if (targetId && fileObj.id) {
          await bindAttachmentMutation.mutateAsync({
            data: {
              fileId: fileObj.id,
              targetType,
              targetId,
              attachmentType: 2, // Gallery = 2
              sort: sortIndex,
            },
          })
        }

        newItems.push({
          id: fileObj.id || generateUniqueId(),
          url: fileObj.accessUrl || URL.createObjectURL(file),
          name: file.name,
          sort: sortIndex,
        })
      }

      const updated = [...gallery, ...newItems].slice(0, maxCount)
      setGallery(updated)
      if (onChange) onChange(updated)
    } catch {
      // 防错
    } finally {
      setUploading(false)
    }
  }

  const handleRemove = (id: string) => {
    const updated = gallery.filter((x) => x.id !== id)
    setGallery(updated)
    if (onChange) onChange(updated)
  }

  return (
    <div className='space-y-3'>
      <div className='grid grid-cols-4 sm:grid-cols-5 gap-3'>
        {gallery.map((item, idx) => (
          <div
            key={item.id}
            className='group relative aspect-square border rounded-lg overflow-hidden bg-muted/30 flex items-center justify-center'
          >
            <img
              src={item.url}
              alt={item.name}
              className='w-full h-full object-cover transition-transform group-hover:scale-105'
            />
            <div className='absolute inset-0 bg-black/40 opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center gap-1.5'>
              <Button
                type='button'
                variant='destructive'
                size='icon'
                className='h-7 w-7 rounded-full'
                onClick={() => handleRemove(item.id)}
              >
                <X className='h-3.5 w-3.5' />
              </Button>
            </div>
            <div className='absolute bottom-1 right-1 bg-black/60 text-white font-mono text-[9px] px-1.5 py-0.5 rounded'>
              #{idx + 1}
            </div>
          </div>
        ))}

        {gallery.length < maxCount && (
          <label
            htmlFor={`gallery-input-${targetId}`}
            className={`aspect-square border border-dashed rounded-lg flex flex-col items-center justify-center gap-1.5 hover:border-primary/60 hover:bg-accent/40 transition-colors cursor-pointer text-muted-foreground hover:text-foreground ${
              uploading ? 'pointer-events-none opacity-60' : ''
            }`}
          >
            {uploading ? (
              <Loader2 className='h-5 w-5 animate-spin text-primary' />
            ) : (
              <Plus className='h-5 w-5 text-primary' />
            )}
            <span className='text-[11px] font-medium'>
              {uploading ? t('上传中...') : t('添加图片')}
            </span>
            <input
              id={`gallery-input-${targetId}`}
              type='file'
              accept='image/*'
              multiple
              className='hidden'
              onChange={handleAddFiles}
              disabled={uploading}
            />
          </label>
        )}
      </div>

      <div className='flex items-center gap-2 text-xs text-muted-foreground'>
        <ImageIcon className='h-3.5 w-3.5' />
        <span>
          {t('已添加')} {gallery.length} / {maxCount} {t('张图片')}
        </span>
      </div>
    </div>
  )
}
