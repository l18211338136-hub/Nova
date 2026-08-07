export interface Area {
  x: number
  y: number
  width: number
  height: number
}

export async function getCroppedImg(
  imageSrc: string,
  pixelCrop: Area,
  fileName = 'avatar.png'
): Promise<{ file: File; previewUrl: string; width: number; height: number; sizeBytes: number }> {
  const image = await createImage(imageSrc)
  const canvas = document.createElement('canvas')
  const ctx = canvas.getContext('2d')

  if (!ctx) {
    throw new Error('No 2d context')
  }

  canvas.width = pixelCrop.width
  canvas.height = pixelCrop.height

  ctx.drawImage(
    image,
    pixelCrop.x,
    pixelCrop.y,
    pixelCrop.width,
    pixelCrop.height,
    0,
    0,
    pixelCrop.width,
    pixelCrop.height
  )

  return new Promise((resolve, reject) => {
    canvas.toBlob((blob) => {
      if (!blob) {
        reject(new Error('Canvas is empty'))
        return
      }
      const file = new File([blob], fileName, { type: blob.type || 'image/png' })
      const previewUrl = URL.createObjectURL(blob)
      resolve({
        file,
        previewUrl,
        width: Math.round(pixelCrop.width),
        height: Math.round(pixelCrop.height),
        sizeBytes: blob.size,
      })
    }, 'image/png')
  })
}

function createImage(url: string): Promise<HTMLImageElement> {
  return new Promise((resolve, reject) => {
    const image = new Image()
    image.addEventListener('load', () => resolve(image))
    image.addEventListener('error', (error) => reject(error))
    image.setAttribute('crossOrigin', 'anonymous')
    image.src = url
  })
}
