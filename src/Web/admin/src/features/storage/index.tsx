import React, { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import {
  useStorageFiles,
  useStorageStats,
  useUploadStorageFile,
  useDeleteStorageFile,
  useReplaceStorageFileContent,
} from '@/api/endpoints/storage';
import { type StorageFileDto as StorageFileDtoType } from '@/api/model';
import { Header } from '@/components/layout/header';
import { Main } from '@/components/layout/main';
import { Search as TopSearch } from '@/components/search';
import { ThemeSwitch } from '@/components/theme-switch';
import { ConfigDrawer } from '@/components/config-drawer';
import { ProfileDropdown } from '@/components/profile-dropdown';
import { FileCard, formatFileSize } from './components/file-card';
import { FilePreviewModal } from './components/file-preview-modal';
import { StoragePagination } from './components/storage-pagination';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import {
  HardDrive,
  Files,
  UploadCloud,
  Search,
  Sparkles,
  Loader2,
  Trash2,
  RefreshCw,
  AlertCircle,
} from 'lucide-react';
import { toast } from 'sonner';

type StorageFileDto = NonNullable<StorageFileDtoType>;

export default function StorageFeature() {
  const { t } = useTranslation();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(8); // 默认 8 项/页
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [category, setCategory] = useState('all');

  const [previewFile, setPreviewFile] = useState<StorageFileDto | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<StorageFileDto | null>(null);
  const [replaceTarget, setReplaceTarget] = useState<StorageFileDto | null>(null);
  const [uploadOpen, setUploadOpen] = useState(false);

  // 300ms 防抖搜索，避免按键频繁触发接口请求与UI闪烁
  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearch(search);
      setPage(1);
    }, 300);

    return () => clearTimeout(timer);
  }, [search]);

  // 使用 TanStack Query placeholderData 机制保持前次数据，无缝无闪烁更新
  const {
    data: filesResponse,
    isLoading,
    isFetching,
    refetch: refetchFiles,
  } = useStorageFiles(
    {
      page,
      pageSize,
      search: debouncedSearch || undefined,
      category: category === 'all' ? undefined : category,
    },
    {
      query: {
        placeholderData: (previousData) => previousData,
      },
    }
  );

  const { data: statsResponse, refetch: refetchStats } = useStorageStats();
  const uploadMutation = useUploadStorageFile();
  const deleteMutation = useDeleteStorageFile();
  const replaceMutation = useReplaceStorageFileContent();

  // 解析 Orval 接口返回数据
  const filesResult = filesResponse?.data;
  const files: StorageFileDto[] = (filesResult?.items || []) as StorageFileDto[];
  const total = filesResult?.total || 0;

  const statsRaw = statsResponse?.data as Record<string, unknown> | undefined;
  const stats = {
    totalFiles: (statsRaw?.totalFiles as number) || 0,
    totalSize: (statsRaw?.totalSize as number) || 0,
    activeProvider: (statsRaw?.activeProvider as string) || 'Local',
  };

  const handleFileUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFiles = e.target.files;
    if (!selectedFiles || selectedFiles.length === 0) return;

    let successCount = 0;

    for (let i = 0; i < selectedFiles.length; i++) {
      try {
        await uploadMutation.mutateAsync({
          data: {
            file: selectedFiles[i],
          },
        });
        successCount++;
      } catch {
        toast.error(t('Failed to upload file', { name: selectedFiles[i].name }));
      }
    }

    // 重置 input 框 value，以便能重复选择上传同名文件
    e.target.value = '';

    if (successCount > 0) {
      toast.success(t('Successfully uploaded {{count}} files', { count: successCount }));
      setUploadOpen(false);
      setPage(1); // 自动回到第 1 页展示按时间倒序排列的最最新上传文件
      setCategory('all'); // 重置分类过滤器，防止因切在其他 Tab 导致上传文件“不显示”
      refetchFiles();
      refetchStats();
    }
  };

  const handleReplaceFile = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFile = e.target.files?.[0];
    if (!selectedFile || !replaceTarget || !replaceTarget.id) return;

    const targetExt = (replaceTarget.fileName || '').split('.').pop()?.toLowerCase() || '';
    const newExt = (selectedFile.name || '').split('.').pop()?.toLowerCase() || '';

    if (targetExt !== newExt) {
      toast.error(t('Replace file failed: Must upload a file with extension .{{ext}}', { ext: targetExt.toUpperCase() }));
      e.target.value = '';
      return;
    }

    try {
      await replaceMutation.mutateAsync({
        id: replaceTarget.id,
        data: {
          file: selectedFile,
        },
      });
      toast.success(t('Operation successful'));
      setReplaceTarget(null);
      refetchFiles();
      refetchStats();
    } catch {
      toast.error(t('Failed to replace file'));
    } finally {
      e.target.value = '';
    }
  };

  const confirmDelete = async () => {
    if (!deleteTarget || !deleteTarget.id) return;
    try {
      await deleteMutation.mutateAsync({ id: deleteTarget.id });
      toast.success(t('File deleted successfully'));
      setDeleteTarget(null);
      refetchFiles();
      refetchStats();
    } catch {
      toast.error(t('Failed to delete file'));
    }
  };

  return (
    <>
      {/* 统一的规范 Header 顶部导航 */}
      <Header fixed>
        <TopSearch className='me-auto' />
        <ThemeSwitch />
        <ConfigDrawer />
        <ProfileDropdown />
      </Header>

      {/* 与 User/Role 列表页统一宽度、外边距与内边距规范的 Main 容器 */}
      <Main className='flex flex-1 flex-col gap-4 sm:gap-6'>
        {/* 顶部 Header 与 标题 */}
        <div className='flex flex-wrap items-end justify-between gap-2'>
          <div>
            <h2 className='text-2xl font-bold tracking-tight text-foreground flex items-center gap-2.5'>
              <HardDrive className='w-6 h-6 text-primary' /> {t('File Resource Management')}
            </h2>
            <p className='text-xs text-muted-foreground mt-1'>
              {t('Card-based storage resource management with online preview.')}
            </p>
          </div>

          <Button
            onClick={() => setUploadOpen(true)}
            className='gap-2 bg-primary hover:bg-primary/90 text-primary-foreground shadow-md rounded-xl px-4 py-2 text-xs font-semibold'
          >
            <UploadCloud className='w-4 h-4' /> {t('Upload New File')}
          </Button>
        </div>

        {/* 存储容量统计指标面板 */}
        <div className='grid grid-cols-1 sm:grid-cols-3 gap-4'>
          <div className='p-4 rounded-xl border border-border/60 bg-card shadow-sm flex items-center gap-3.5'>
            <div className='p-2.5 rounded-xl bg-blue-500/10 text-blue-600 dark:text-blue-400 border border-blue-500/20'>
              <Files className='w-5 h-5' />
            </div>
            <div>
              <p className='text-[11px] font-medium text-muted-foreground'>{t('Total Files')}</p>
              <h3 className='text-lg font-bold text-foreground mt-0.5'>{stats.totalFiles}</h3>
            </div>
          </div>

          <div className='p-4 rounded-xl border border-border/60 bg-card shadow-sm flex items-center gap-3.5'>
            <div className='p-2.5 rounded-xl bg-purple-500/10 text-purple-600 dark:text-purple-400 border border-purple-500/20'>
              <HardDrive className='w-5 h-5' />
            </div>
            <div>
              <p className='text-[11px] font-medium text-muted-foreground'>{t('Total Storage Used')}</p>
              <h3 className='text-lg font-bold text-foreground mt-0.5'>
                {formatFileSize(stats.totalSize)}
              </h3>
            </div>
          </div>

          <div className='p-4 rounded-xl border border-border/60 bg-card shadow-sm flex items-center gap-3.5'>
            <div className='p-2.5 rounded-xl bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border border-emerald-500/20'>
              <Sparkles className='w-5 h-5' />
            </div>
            <div>
              <p className='text-[11px] font-medium text-muted-foreground'>{t('Storage Driver')}</p>
              <h3 className='text-lg font-bold text-foreground mt-0.5 capitalize'>
                {stats.activeProvider}
              </h3>
            </div>
          </div>
        </div>

        {/* 搜索与分类 Tabs 过滤栏 */}
        <div className='flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-4 pt-1'>
          <div className='relative flex-1 max-w-sm'>
            {isFetching && search !== debouncedSearch ? (
              <Loader2 className='absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-primary animate-spin' />
            ) : (
              <Search className='absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground' />
            )}
            <Input
              placeholder={t('Search file name...')}
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className='pl-9 rounded-xl bg-card border-border/60 text-xs focus-visible:ring-primary h-9'
            />
          </div>

          <Tabs
            value={category}
            onValueChange={(val) => {
              setCategory(val);
              setPage(1);
            }}
            className='w-full sm:w-auto overflow-x-auto'
          >
            <TabsList className='bg-muted/60 p-1 rounded-xl flex items-center gap-1 h-9'>
              <TabsTrigger value='all' className='rounded-lg text-xs px-3 py-1'>
                {t('All')}
              </TabsTrigger>
              <TabsTrigger value='image' className='rounded-lg text-xs px-3 py-1'>
                {t('Images')}
              </TabsTrigger>
              <TabsTrigger value='video' className='rounded-lg text-xs px-3 py-1'>
                {t('Videos')}
              </TabsTrigger>
              <TabsTrigger value='audio' className='rounded-lg text-xs px-3 py-1'>
                {t('Audios')}
              </TabsTrigger>
              <TabsTrigger value='document' className='rounded-lg text-xs px-3 py-1'>
                {t('Documents')}
              </TabsTrigger>
              <TabsTrigger value='archive' className='rounded-lg text-xs px-3 py-1'>
                {t('Archives')}
              </TabsTrigger>
            </TabsList>
          </Tabs>
        </div>

        {/* 卡片响应式网格列表 */}
        {isLoading && !filesResponse ? (
          <div className='grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4 py-4'>
            {Array.from({ length: pageSize }).map((_, i) => (
              <div
                key={i}
                className='h-52 rounded-xl border border-border/40 bg-card/40 animate-pulse'
              />
            ))}
          </div>
        ) : files.length === 0 ? (
          <div className='py-16 text-center flex flex-col items-center justify-center border border-dashed border-border/60 rounded-2xl bg-card/20'>
            <div className='p-3.5 rounded-full bg-primary/10 text-primary border border-primary/20 mb-3'>
              <Files className='w-8 h-8 opacity-70' />
            </div>
            <h3 className='text-sm font-semibold text-foreground'>{t('No matching files found.')}</h3>
            <p className='text-xs text-muted-foreground mt-1 max-w-sm'>
              {t("No files found, click 'Upload New File' above to add.")}
            </p>
          </div>
        ) : (
          <div className={`grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4 transition-opacity duration-200 ${isFetching ? 'opacity-80' : 'opacity-100'}`}>
            {files.map((file, idx) => (
              <FileCard
                key={file.id || idx}
                file={file}
                onPreview={(f) => setPreviewFile(f)}
                onReplace={(f) => setReplaceTarget(f)}
                onDelete={(f) => setDeleteTarget(f)}
              />
            ))}
          </div>
        )}

        {/* 用户列表同款底部固定的 StoragePagination 分页组件 */}
        <StoragePagination
          currentPage={page}
          pageSize={pageSize}
          total={total}
          onPageChange={(p) => setPage(p)}
          onPageSizeChange={(sz) => {
            setPageSize(sz);
            setPage(1);
          }}
        />
      </Main>

      {/* 多功能预览 Modal */}
      <FilePreviewModal
        file={previewFile}
        isOpen={!!previewFile}
        onClose={() => setPreviewFile(null)}
      />

      {/* 覆盖上传 / 内容替换对话框 */}
      <Dialog open={!!replaceTarget} onOpenChange={(open) => !open && setReplaceTarget(null)}>
        <DialogContent className='sm:max-w-md p-6 rounded-2xl bg-card border-border shadow-2xl'>
          <DialogHeader>
            <DialogTitle className='text-base font-bold flex items-center gap-2'>
              <RefreshCw className='w-5 h-5 text-primary' /> {t('Replace File Content')}
            </DialogTitle>
          </DialogHeader>

          <div className='space-y-4 pt-1'>
            <div className='p-3.5 rounded-xl bg-amber-500/10 border border-amber-500/20 flex items-start gap-2.5 text-xs text-amber-700 dark:text-amber-300'>
              <AlertCircle className='w-4 h-4 shrink-0 mt-0.5' />
              <div>
                <p className='font-semibold'>{t('Same extension replace rule')}</p>
                <p className='mt-0.5 opacity-90'>
                  {t('Current file is .{{ext}} type. Access URL will remain unchanged.', { ext: ((replaceTarget?.fileName || '').split('.').pop() || '').toUpperCase() })}
                </p>
              </div>
            </div>

            <div className='flex flex-col items-center justify-center p-7 border-2 border-dashed border-primary/30 rounded-xl bg-primary/5 hover:bg-primary/10 transition-colors cursor-pointer group relative'>
              <input
                type='file'
                accept={`.${((replaceTarget?.fileName || '').split('.').pop() || '').toLowerCase()}`}
                onChange={handleReplaceFile}
                className='absolute inset-0 opacity-0 cursor-pointer z-10'
                disabled={replaceMutation.isPending}
              />
              {replaceMutation.isPending ? (
                <div className='flex flex-col items-center gap-2 text-primary'>
                  <Loader2 className='w-8 h-8 animate-spin' />
                  <span className='text-xs font-medium'>{t('Replacing file content, please wait...')}</span>
                </div>
              ) : (
                <div className='flex flex-col items-center text-center gap-2'>
                  <div className='p-3 rounded-full bg-primary/10 text-primary group-hover:scale-110 transition-transform'>
                    <RefreshCw className='w-6 h-6' />
                  </div>
                  <div>
                    <p className='text-xs font-semibold text-foreground'>
                      {t('Click or drag new .{{ext}} file here', { ext: ((replaceTarget?.fileName || '').split('.').pop() || '').toUpperCase() })}
                    </p>
                    <p className='text-[11px] text-muted-foreground mt-1'>
                      {t('Click to replace original file content')}
                    </p>
                  </div>
                </div>
              )}
            </div>
          </div>
        </DialogContent>
      </Dialog>

      {/* 上传 Dropzone 对话框 */}
      <Dialog open={uploadOpen} onOpenChange={setUploadOpen}>
        <DialogContent className='sm:max-w-md p-6 rounded-2xl bg-card border-border shadow-2xl'>
          <DialogHeader>
            <DialogTitle className='text-base font-bold flex items-center gap-2'>
              <UploadCloud className='w-5 h-5 text-primary' /> {t('Upload File')}
            </DialogTitle>
          </DialogHeader>

          <div className='mt-3 flex flex-col items-center justify-center p-8 border-2 border-dashed border-primary/30 rounded-xl bg-primary/5 hover:bg-primary/10 transition-colors cursor-pointer group relative'>
            <input
              type='file'
              multiple
              onChange={handleFileUpload}
              className='absolute inset-0 opacity-0 cursor-pointer z-10'
              disabled={uploadMutation.isPending}
            />
            {uploadMutation.isPending ? (
              <div className='flex flex-col items-center gap-2 text-primary'>
                <Loader2 className='w-8 h-8 animate-spin' />
                <span className='text-xs font-medium'>{t('Uploading to storage, please wait...')}</span>
              </div>
            ) : (
              <div className='flex flex-col items-center text-center gap-2'>
                <div className='p-3 rounded-full bg-primary/10 text-primary group-hover:scale-110 transition-transform'>
                  <UploadCloud className='w-7 h-7' />
                </div>
                <div>
                  <p className='text-xs font-semibold text-foreground'>
                    {t('Click or drag files here to upload')}
                  </p>
                  <p className='text-[11px] text-muted-foreground mt-1'>
                    {t('Supports images, videos, audios, documents and archives')}
                  </p>
                </div>
              </div>
            )}
          </div>
        </DialogContent>
      </Dialog>

      {/* 删除确认对话框 */}
      <Dialog open={!!deleteTarget} onOpenChange={(open) => !open && setDeleteTarget(null)}>
        <DialogContent className='sm:max-w-sm p-6 rounded-2xl bg-card border-border shadow-2xl gap-3'>
          <DialogHeader>
            <DialogTitle className='text-base font-bold text-red-600 dark:text-red-400 flex items-center gap-2'>
              <Trash2 className='w-4 h-4' /> {t('Confirm Delete File')}
            </DialogTitle>
          </DialogHeader>
          <div className='text-xs text-muted-foreground'>
            {t('Delete file will physically remove it and its database record:')}
            <p className='font-semibold text-foreground mt-1 truncate'>{deleteTarget?.fileName}</p>
          </div>
          <div className='flex items-center justify-end gap-2 pt-2'>
            <Button size='sm' variant='outline' onClick={() => setDeleteTarget(null)} className='h-8 text-xs'>
              {t('Cancel')}
            </Button>
            <Button size='sm' variant='destructive' onClick={confirmDelete} className='h-8 text-xs'>
              {t('Confirm Delete')}
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </>
  );
}
