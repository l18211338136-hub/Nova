import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { type StorageFileDto as StorageFileDtoType } from '@/api/model';
import {
  FileText,
  Video,
  Music,
  Archive,
  Eye,
  Copy,
  Download,
  Trash2,
  Check,
  FileCode,
  FileSpreadsheet,
  RefreshCw,
} from 'lucide-react';
import { toast } from 'sonner';

export type StorageFileDto = NonNullable<StorageFileDtoType>;

interface FileCardProps {
  file: StorageFileDto;
  onPreview: (file: StorageFileDto) => void;
  onReplace?: (file: StorageFileDto) => void;
  onDelete: (file: StorageFileDto) => void;
}

export const formatFileSize = (bytes?: number): string => {
  if (!bytes || bytes === 0) return '0 B';
  const k = 1024;
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
};

export const getFileCategory = (contentType?: string | null, fileName?: string | null) => {
  const type = (contentType || '').toLowerCase();
  const ext = (fileName || '').split('.').pop()?.toLowerCase() || '';

  if (type.startsWith('image/') || ['png', 'jpg', 'jpeg', 'gif', 'webp', 'svg'].includes(ext)) {
    return 'image';
  }
  if (type.startsWith('video/') || ['mp4', 'webm', 'mov', 'avi', 'mkv'].includes(ext)) {
    return 'video';
  }
  if (type.startsWith('audio/') || ['mp3', 'wav', 'ogg', 'flac'].includes(ext)) {
    return 'audio';
  }
  if (
    type.includes('pdf') ||
    type.includes('word') ||
    type.includes('document') ||
    type.includes('text') ||
    type.includes('json') ||
    type.includes('sheet') ||
    type.includes('excel') ||
    ['pdf', 'doc', 'docx', 'txt', 'md', 'json', 'xlsx', 'csv', 'cs', 'ts', 'js'].includes(ext)
  ) {
    return 'document';
  }
  if (
    type.includes('zip') ||
    type.includes('rar') ||
    type.includes('7z') ||
    type.includes('tar') ||
    ['zip', 'rar', '7z', 'tar', 'gz'].includes(ext)
  ) {
    return 'archive';
  }
  return 'other';
};

export const FileCard: React.FC<FileCardProps> = ({ file, onPreview, onReplace, onDelete }) => {
  const { t } = useTranslation();
  const [copied, setCopied] = useState(false);
  const category = getFileCategory(file.contentType, file.fileName);

  const getFullUrl = (url?: string | null) => {
    if (!url) return '';
    if (url.startsWith('http://') || url.startsWith('https://')) return url;
    const baseUrl = import.meta.env.VITE_API_URL || '';
    return `${baseUrl}${url.startsWith('/') ? '' : '/'}${url}`;
  };

  const fullAccessUrl = getFullUrl(file.accessUrl);

  const handleCopy = (e: React.MouseEvent) => {
    e.stopPropagation();
    if (!fullAccessUrl) return;
    navigator.clipboard.writeText(fullAccessUrl);
    setCopied(true);
    toast.success(t('File direct link copied to clipboard'));
    setTimeout(() => setCopied(false), 2000);
  };

  const handleDownload = (e: React.MouseEvent) => {
    e.stopPropagation();
    if (!fullAccessUrl) return;
    const a = document.createElement('a');
    a.href = fullAccessUrl;
    a.download = file.fileName || 'download';
    a.target = '_blank';
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
  };

  const renderBadge = () => {
    switch (category) {
      case 'image':
        return <span className='px-2.5 py-0.5 text-[11px] font-semibold rounded-full bg-emerald-500/15 text-emerald-600 dark:text-emerald-400 border border-emerald-500/30 backdrop-blur-md shadow-sm'>{t('Images')}</span>;
      case 'video':
        return <span className='px-2.5 py-0.5 text-[11px] font-semibold rounded-full bg-purple-500/15 text-purple-600 dark:text-purple-400 border border-purple-500/30 backdrop-blur-md shadow-sm'>{t('Videos')}</span>;
      case 'audio':
        return <span className='px-2.5 py-0.5 text-[11px] font-semibold rounded-full bg-pink-500/15 text-pink-600 dark:text-pink-400 border border-pink-500/30 backdrop-blur-md shadow-sm'>{t('Audios')}</span>;
      case 'document':
        return <span className='px-2.5 py-0.5 text-[11px] font-semibold rounded-full bg-blue-500/15 text-blue-600 dark:text-blue-400 border border-blue-500/30 backdrop-blur-md shadow-sm'>{t('Documents')}</span>;
      case 'archive':
        return <span className='px-2.5 py-0.5 text-[11px] font-semibold rounded-full bg-amber-500/15 text-amber-600 dark:text-amber-400 border border-amber-500/30 backdrop-blur-md shadow-sm'>{t('Archives')}</span>;
      default:
        return <span className='px-2.5 py-0.5 text-[11px] font-semibold rounded-full bg-gray-500/15 text-gray-600 dark:text-gray-400 border border-gray-500/30 backdrop-blur-md shadow-sm'>{t('File')}</span>;
    }
  };

  const renderPreviewBox = () => {
    if (category === 'image' && fullAccessUrl) {
      return (
        <div className='relative w-full h-40 bg-slate-900/5 dark:bg-slate-900/40 flex items-center justify-center overflow-hidden'>
          <img
            src={fullAccessUrl}
            alt={file.fileName || 'image'}
            className='w-full h-full object-cover group-hover:scale-105 transition-transform duration-500 ease-out'
            onError={(e) => {
              (e.target as HTMLElement).style.display = 'none';
            }}
          />
        </div>
      );
    }

    if (category === 'video') {
      return (
        <div className='w-full h-40 bg-gradient-to-br from-purple-950/20 via-slate-900/30 to-indigo-950/20 flex flex-col items-center justify-center text-purple-500 dark:text-purple-400'>
          <div className='p-3 rounded-full bg-purple-500/10 border border-purple-500/20 mb-1.5 group-hover:scale-110 transition-transform'>
            <Video className='w-7 h-7' />
          </div>
          <span className='text-[11px] font-medium text-purple-600 dark:text-purple-300'>{t('Video File')}</span>
        </div>
      );
    }

    if (category === 'audio') {
      return (
        <div className='w-full h-40 bg-gradient-to-br from-pink-950/20 via-slate-900/30 to-rose-950/20 flex flex-col items-center justify-center text-pink-500 dark:text-pink-400'>
          <div className='p-3 rounded-full bg-pink-500/10 border border-pink-500/20 mb-1.5 group-hover:scale-110 transition-transform'>
            <Music className='w-7 h-7' />
          </div>
          <span className='text-[11px] font-medium text-pink-600 dark:text-pink-300'>{t('Audio File')}</span>
        </div>
      );
    }

    if (category === 'document') {
      const ext = (file.fileName || '').split('.').pop()?.toLowerCase();
      let Icon = FileText;
      let colorClass = 'text-blue-500 bg-blue-500/10 border-blue-500/20';

      if (['json', 'cs', 'ts', 'js', 'html', 'css'].includes(ext || '')) {
        Icon = FileCode;
        colorClass = 'text-cyan-500 bg-cyan-500/10 border-cyan-500/20';
      } else if (['xlsx', 'xls', 'csv'].includes(ext || '')) {
        Icon = FileSpreadsheet;
        colorClass = 'text-emerald-500 bg-emerald-500/10 border-emerald-500/20';
      }

      return (
        <div className='w-full h-40 bg-gradient-to-br from-blue-950/10 via-slate-900/20 to-slate-900/30 flex flex-col items-center justify-center text-blue-500'>
          <div className={`p-3 rounded-full border mb-1.5 group-hover:scale-110 transition-transform ${colorClass}`}>
            <Icon className='w-7 h-7' />
          </div>
          <span className='text-[11px] font-medium uppercase tracking-wider text-muted-foreground'>
            {ext || 'DOC'}
          </span>
        </div>
      );
    }

    if (category === 'archive') {
      return (
        <div className='w-full h-40 bg-gradient-to-br from-amber-950/20 via-slate-900/30 to-orange-950/20 flex flex-col items-center justify-center text-amber-500'>
          <div className='p-3 rounded-full bg-amber-500/10 border border-amber-500/20 mb-1.5 group-hover:scale-110 transition-transform'>
            <Archive className='w-7 h-7' />
          </div>
          <span className='text-[11px] font-medium uppercase tracking-wider text-amber-600 dark:text-amber-400'>
            ARCHIVE
          </span>
        </div>
      );
    }

    return (
      <div className='w-full h-40 bg-slate-900/5 dark:bg-slate-900/30 flex flex-col items-center justify-center text-gray-400'>
        <FileText className='w-8 h-8 mb-1.5 opacity-60 group-hover:scale-110 transition-transform' />
        <span className='text-[11px] text-muted-foreground'>{t('File')}</span>
      </div>
    );
  };

  return (
    <div
      onClick={() => onPreview(file)}
      className='group relative rounded-xl border border-border/60 bg-card hover:bg-accent/30 shadow-sm hover:shadow-lg hover:border-primary/40 transition-all duration-300 overflow-hidden flex flex-col cursor-pointer'
    >
      {/* 顶部预览图片 / 图标 Box */}
      <div className='relative overflow-hidden border-b border-border/40'>
        {renderPreviewBox()}
        <div className='absolute top-2.5 left-2.5 z-10'>{renderBadge()}</div>

        {/* 悬浮黑色半透明 Mask 快捷按钮 */}
        <div className='absolute inset-0 bg-slate-950/50 backdrop-blur-[2px] opacity-0 group-hover:opacity-100 transition-opacity duration-300 flex items-center justify-center gap-1.5 p-3 z-20'>
          <button
            onClick={(e) => {
              e.stopPropagation();
              onPreview(file);
            }}
            className='p-2 rounded-full bg-white/90 text-slate-900 hover:bg-white hover:scale-110 transition-all shadow-md'
            title={t('View')}
          >
            <Eye className='w-3.5 h-3.5' />
          </button>

          {onReplace && (
            <button
              onClick={(e) => {
                e.stopPropagation();
                onReplace(file);
              }}
              className='p-2 rounded-full bg-white/90 text-slate-900 hover:bg-white hover:scale-110 transition-all shadow-md'
              title='覆盖替换内容'
            >
              <RefreshCw className='w-3.5 h-3.5' />
            </button>
          )}

          <button
            onClick={handleCopy}
            className='p-2 rounded-full bg-white/90 text-slate-900 hover:bg-white hover:scale-110 transition-all shadow-md'
            title={t('Copy Link')}
          >
            {copied ? <Check className='w-3.5 h-3.5 text-emerald-600' /> : <Copy className='w-3.5 h-3.5' />}
          </button>

          <button
            onClick={handleDownload}
            className='p-2 rounded-full bg-white/90 text-slate-900 hover:bg-white hover:scale-110 transition-all shadow-md'
            title={t('Download')}
          >
            <Download className='w-3.5 h-3.5' />
          </button>

          <button
            onClick={(e) => {
              e.stopPropagation();
              onDelete(file);
            }}
            className='p-2 rounded-full bg-red-500/90 text-white hover:bg-red-600 hover:scale-110 transition-all shadow-md'
            title={t('Delete')}
          >
            <Trash2 className='w-3.5 h-3.5' />
          </button>
        </div>
      </div>

      {/* 底部文件属性与名称 */}
      <div className='p-3.5 flex flex-col justify-between flex-1 gap-1.5'>
        <h4
          className='text-xs font-semibold text-foreground truncate w-full tracking-tight'
          title={file.fileName || ''}
        >
          {file.fileName}
        </h4>

        <div className='flex items-center justify-between text-[11px] text-muted-foreground pt-1 border-t border-border/30'>
          <span className='font-medium'>{formatFileSize(file.fileSize)}</span>
        </div>
      </div>
    </div>
  );
};
