import React from 'react';
import { useTranslation } from 'react-i18next';
import { type StorageFileDto as StorageFileDtoType } from '@/api/model';
import { getFileCategory, formatFileSize } from './file-card';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Download, Copy, FileText, Music, FileCode, FileArchive } from 'lucide-react';
import { toast } from 'sonner';

type StorageFileDto = NonNullable<StorageFileDtoType>;

interface FilePreviewModalProps {
  file: StorageFileDto | null;
  isOpen: boolean;
  onClose: () => void;
}

export const FilePreviewModal: React.FC<FilePreviewModalProps> = ({ file, isOpen, onClose }) => {
  const { t } = useTranslation();
  if (!file) return null;

  const category = getFileCategory(file.contentType, file.fileName);
  const ext = (file.fileName || '').split('.').pop()?.toUpperCase() || 'FILE';

  const getFullUrl = (url?: string | null) => {
    if (!url) return '';
    if (url.startsWith('http://') || url.startsWith('https://')) return url;
    const baseUrl = import.meta.env.VITE_API_URL || '';
    return `${baseUrl}${url.startsWith('/') ? '' : '/'}${url}`;
  };

  const fullUrl = getFullUrl(file.accessUrl);

  const handleCopyLink = () => {
    if (!fullUrl) return;
    navigator.clipboard.writeText(fullUrl);
    toast.success(t('File direct link copied to clipboard'));
  };

  const handleDownload = () => {
    if (!fullUrl) return;
    const a = document.createElement('a');
    a.href = fullUrl;
    a.download = file.fileName || 'download';
    a.target = '_blank';
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
  };

  // 简化的格式显示文本
  const getFormatLabel = () => {
    if (category === 'image') return t('Image File');
    if (category === 'video') return t('Video File');
    if (category === 'audio') return t('Audio File');
    if (category === 'document') return t('Document', { ext });
    if (category === 'archive') return t('Archive', { ext });
    return `${ext} ${t('File')}`;
  };

  const renderPreviewContent = () => {
    if (!fullUrl) {
      return (
        <div className='py-12 text-center text-muted-foreground text-xs'>
          {t('No content.')}
        </div>
      );
    }

    if (category === 'image') {
      return (
        <div className='flex items-center justify-center p-2 rounded-xl bg-slate-950/90 max-h-[65vh] overflow-hidden border border-border/40 shadow-inner'>
          <img
            src={fullUrl}
            alt={file.fileName || 'image'}
            className='max-h-[60vh] w-auto max-w-full object-contain rounded-lg shadow-lg'
          />
        </div>
      );
    }

    if (category === 'video') {
      return (
        <div className='flex items-center justify-center p-2 rounded-xl bg-black/90 max-h-[65vh] overflow-hidden border border-purple-500/20 shadow-inner'>
          <video
            src={fullUrl}
            controls
            autoPlay
            className='max-h-[60vh] w-full rounded-lg'
          >
            {t('Your browser does not support video playback.')}
          </video>
        </div>
      );
    }

    if (category === 'audio') {
      return (
        <div className='flex flex-col items-center justify-center py-10 px-6 bg-gradient-to-br from-slate-900 via-purple-950/30 to-pink-950/30 rounded-xl border border-pink-500/20 gap-5 text-center shadow-inner'>
          <div className='p-4 rounded-full bg-pink-500/10 border border-pink-500/30 text-pink-400 animate-pulse shadow-lg'>
            <Music className='w-10 h-10' />
          </div>
          <div>
            <h3 className='text-sm font-semibold text-foreground mb-1 max-w-md truncate'>{file.fileName}</h3>
            <p className='text-xs text-muted-foreground'>{formatFileSize(file.fileSize)}</p>
          </div>
          <audio src={fullUrl} controls autoPlay className='w-full max-w-md shadow-md rounded-lg'>
            {t('Your browser does not support audio playback.')}
          </audio>
        </div>
      );
    }

    if (category === 'document' && (file.fileName || '').toLowerCase().endsWith('.pdf')) {
      return (
        <div className='w-full h-[62vh] rounded-xl overflow-hidden border border-border/60 bg-background shadow-inner'>
          <iframe src={fullUrl} title={file.fileName || ''} className='w-full h-full border-none' />
        </div>
      );
    }

    // 非内置预览格式（如 Office docx/xlsx, zip 等）的精致响应式卡片
    let Icon = FileText;
    if (category === 'archive') Icon = FileArchive;
    if (['JSON', 'CS', 'TS', 'JS', 'HTML', 'CSS', 'PY'].includes(ext)) Icon = FileCode;

    return (
      <div className='flex flex-col items-center justify-center py-10 px-6 bg-muted/40 rounded-xl border border-border/40 text-center space-y-4'>
        <div className='p-4 rounded-2xl bg-primary/10 text-primary border border-primary/20 shadow-sm'>
          <Icon className='w-10 h-10' />
        </div>

        <div className='space-y-1.5 max-w-md'>
          <h4 className='font-semibold text-sm text-foreground'>
            {t('Preview Not Supported', { ext })}
          </h4>
          <p className='text-xs text-muted-foreground leading-relaxed'>
            {t('Preview Subtext')}
          </p>
        </div>

        <div className='pt-2 flex items-center justify-center gap-3 w-full sm:w-auto'>
          <Button onClick={handleDownload} size='sm' className='gap-2 rounded-xl px-5 h-9 text-xs font-semibold shadow-sm'>
            <Download className='w-4 h-4' /> {t('Download File to Local')}
          </Button>
          <Button variant='outline' size='sm' onClick={handleCopyLink} className='gap-2 rounded-xl px-4 h-9 text-xs font-medium'>
            <Copy className='w-4 h-4' /> {t('Copy Direct Link')}
          </Button>
        </div>
      </div>
    );
  };

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className='max-w-xl p-6 rounded-2xl bg-card border-border/80 shadow-2xl gap-5'>
        {/* 对话框头部：干净明了的标题与属性说明 */}
        <DialogHeader className='pr-8 pb-3 border-b border-border/40'>
          <DialogTitle className='text-base font-bold text-foreground truncate max-w-md'>
            {file.fileName}
          </DialogTitle>
          <div className='flex items-center gap-2 mt-1 text-xs text-muted-foreground'>
            <span>{formatFileSize(file.fileSize)}</span>
            <span>·</span>
            <span className='font-medium text-foreground/80'>{getFormatLabel()}</span>
          </div>
        </DialogHeader>

        {/* 预览 / 占位展示 */}
        <div className='w-full overflow-hidden'>{renderPreviewContent()}</div>
      </DialogContent>
    </Dialog>
  );
};
