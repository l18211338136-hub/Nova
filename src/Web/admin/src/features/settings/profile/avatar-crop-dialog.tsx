import { useState, useCallback, useEffect } from 'react'
import Cropper from 'react-easy-crop'
import { Area, getCroppedImg } from './crop-image'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from '@/components/ui/dialog'
import { Loader2 } from 'lucide-react'

interface AvatarCropDialogProps {
  open: boolean
  imageSrc: string | null
  fileName?: string
  onClose: () => void
  onConfirm: (croppedFile: File, previewUrl: string) => Promise<void> | void
}

export function AvatarCropDialog({
  open,
  imageSrc,
  fileName = 'avatar.png',
  onClose,
  onConfirm,
}: AvatarCropDialogProps) {
  const [crop, setCrop] = useState({ x: 0, y: 0 })
  const [zoom, setZoom] = useState(1)
  const [croppedAreaPixels, setCroppedAreaPixels] = useState<Area | null>(null)
  const [previewInfo, setPreviewInfo] = useState<{
    previewUrl: string
    width: number
    height: number
    sizeBytes: number
  } | null>(null)
  const [isProcessing, setIsProcessing] = useState(false)

  const onCropComplete = useCallback(
    async (_croppedArea: Area, pixelCrop: Area) => {
      setCroppedAreaPixels(pixelCrop)
      if (imageSrc) {
        try {
          const res = await getCroppedImg(imageSrc, pixelCrop, fileName)
          setPreviewInfo({
            previewUrl: res.previewUrl,
            width: res.width,
            height: res.height,
            sizeBytes: res.sizeBytes,
          })
        } catch {
          // ignore transient render errors
        }
      }
    },
    [imageSrc, fileName]
  )

  useEffect(() => {
    if (!open) {
      setCrop({ x: 0, y: 0 })
      setZoom(1)
      setCroppedAreaPixels(null)
      setPreviewInfo(null)
    }
  }, [open])

  const handleConfirm = async () => {
    if (!imageSrc || !croppedAreaPixels) return
    try {
      setIsProcessing(true)
      const res = await getCroppedImg(imageSrc, croppedAreaPixels, fileName)
      await onConfirm(res.file, res.previewUrl)
      onClose()
    } catch (e) {
      console.error('Failed to crop avatar:', e)
    } finally {
      setIsProcessing(false)
    }
  }

  const formatFileSize = (bytes: number) => {
    const kb = (bytes / 1024).toFixed(2)
    return `${kb} KB (${bytes} 字节)`
  }

  return (
    <Dialog open={open} onOpenChange={(val) => !val && onClose()}>
      <DialogContent className='max-w-3xl p-6 gap-6'>
        <DialogHeader>
          <DialogTitle className='text-lg font-semibold'>编辑头像</DialogTitle>
          <DialogDescription className='sr-only'>
            拖拽选框及滑动缩放裁剪您的个人头像
          </DialogDescription>
        </DialogHeader>

        <div className='grid grid-cols-1 md:grid-cols-2 gap-6 items-center my-2'>
          {/* 左侧：带棋盘格背景的 Cropper 交互区 */}
          <div className='flex flex-col gap-2'>
            <div className='relative w-full h-[320px] rounded-lg overflow-hidden border bg-neutral-900 checkerboard-bg'>
              {imageSrc && (
                <Cropper
                  image={imageSrc}
                  crop={crop}
                  zoom={zoom}
                  aspect={1}
                  cropShape='round'
                  showGrid={true}
                  onCropChange={setCrop}
                  onZoomChange={setZoom}
                  onCropComplete={onCropComplete}
                />
              )}
            </div>
            <p className='text-xs text-muted-foreground text-center mt-1'>
              温馨提示：滑动滚轮或拖拽放大缩小，右键上方裁剪区可开启功能菜单
            </p>
          </div>

          {/* 右侧：实时圆形预览卡片与图片元数据信息 */}
          <div className='flex flex-col items-center justify-center p-6 border rounded-xl bg-card shadow-sm gap-4 h-[320px]'>
            <div className='relative w-44 h-44 rounded-full border-2 border-border shadow-inner overflow-hidden bg-muted/40 flex items-center justify-center'>
              {previewInfo?.previewUrl ? (
                <img
                  src={previewInfo.previewUrl}
                  alt='Avatar Preview'
                  className='w-full h-full object-cover rounded-full'
                />
              ) : (
                <span className='text-xs text-muted-foreground'>实时预览</span>
              )}
            </div>

            {previewInfo ? (
              <div className='text-center space-y-1 text-xs text-muted-foreground'>
                <p>图像大小：{previewInfo.width} × {previewInfo.height}像素</p>
                <p>文件大小：{formatFileSize(previewInfo.sizeBytes)}</p>
              </div>
            ) : (
              <div className='text-center text-xs text-muted-foreground'>
                调整左侧区域生成预览
              </div>
            )}
          </div>
        </div>

        <DialogFooter className='gap-2 sm:gap-0'>
          <Button variant='outline' onClick={onClose} disabled={isProcessing}>
            取消
          </Button>
          <Button onClick={handleConfirm} disabled={isProcessing || !previewInfo}>
            {isProcessing && <Loader2 className='mr-2 h-4 w-4 animate-spin' />}
            确定
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
