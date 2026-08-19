import React from 'react';
import { useTranslation } from 'react-i18next';
import {
  ChevronLeftIcon,
  ChevronRightIcon,
  DoubleArrowLeftIcon,
  DoubleArrowRightIcon,
} from '@radix-ui/react-icons';
import { getPageNumbers } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';

interface StoragePaginationProps {
  currentPage: number;
  pageSize: number;
  total: number;
  onPageChange: (page: number) => void;
  onPageSizeChange: (pageSize: number) => void;
}

export const StoragePagination: React.FC<StoragePaginationProps> = ({
  currentPage,
  pageSize,
  total,
  onPageChange,
  onPageSizeChange,
}) => {
  const { t } = useTranslation();
  const totalPages = Math.ceil(total / pageSize) || 1;
  const pageNumbers = getPageNumbers(currentPage, totalPages);

  return (
    <div className='flex items-center justify-between overflow-clip px-2 mt-auto border-t border-border/40 pt-4 pb-2 @max-2xl/content:flex-col-reverse @max-2xl/content:gap-4'>
      {/* 左侧：每页行数选择器 (完全与 DataTablePagination 系统文本规范一致) */}
      <div className='flex items-center gap-2'>
        <Select
          value={`${pageSize}`}
          onValueChange={(value) => {
            onPageSizeChange(Number(value));
          }}
        >
          <SelectTrigger className='h-8 w-17.5 text-xs'>
            <SelectValue placeholder={pageSize} />
          </SelectTrigger>
          <SelectContent side='top'>
            {[8, 16, 24, 32, 40, 48].map((size) => (
              <SelectItem key={size} value={`${size}`} className='text-xs'>
                {size}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <p className='hidden text-xs font-medium text-muted-foreground sm:block whitespace-nowrap'>
          {t('Rows per page')}
        </p>
      </div>

      {/* 右侧：页码说明 (Page X of Y) 与 翻页按钮组 */}
      <div className='flex items-center space-x-6 lg:space-x-8 w-full sm:w-auto justify-between sm:justify-end'>
        <div className='flex w-fit items-center justify-center text-xs font-medium text-muted-foreground whitespace-nowrap'>
          {t('Page {{current}} of {{total}}', { current: currentPage, total: totalPages })}
        </div>

        <div className='flex items-center space-x-1.5'>
          {/* 第一页 */}
          <Button
            variant='outline'
            className='size-8 p-0 hidden sm:inline-flex'
            onClick={() => onPageChange(1)}
            disabled={currentPage <= 1}
          >
            <span className='sr-only'>{t('Go to first page')}</span>
            <DoubleArrowLeftIcon className='h-4 w-4' />
          </Button>

          {/* 上一页 */}
          <Button
            variant='outline'
            className='size-8 p-0'
            onClick={() => onPageChange(currentPage - 1)}
            disabled={currentPage <= 1}
          >
            <span className='sr-only'>{t('Go to previous page')}</span>
            <ChevronLeftIcon className='h-4 w-4' />
          </Button>

          {/* 页码数字按钮组 */}
          {pageNumbers.map((pageNumber, index) => (
            <React.Fragment key={`${pageNumber}-${index}`}>
              {pageNumber === '...' ? (
                <span className='px-1 text-xs text-muted-foreground'>...</span>
              ) : (
                <Button
                  variant={currentPage === pageNumber ? 'default' : 'outline'}
                  className='h-8 min-w-8 px-2 text-xs font-medium'
                  onClick={() => onPageChange(pageNumber as number)}
                >
                  <span className='sr-only'>{t('Go to page {{page}}', { page: pageNumber })}</span>
                  {pageNumber}
                </Button>
              )}
            </React.Fragment>
          ))}

          {/* 下一页 */}
          <Button
            variant='outline'
            className='size-8 p-0'
            onClick={() => onPageChange(currentPage + 1)}
            disabled={currentPage >= totalPages}
          >
            <span className='sr-only'>{t('Go to next page')}</span>
            <ChevronRightIcon className='h-4 w-4' />
          </Button>

          {/* 末页 */}
          <Button
            variant='outline'
            className='size-8 p-0 hidden sm:inline-flex'
            onClick={() => onPageChange(totalPages)}
            disabled={currentPage >= totalPages}
          >
            <span className='sr-only'>{t('Go to last page')}</span>
            <DoubleArrowRightIcon className='h-4 w-4' />
          </Button>
        </div>
      </div>
    </div>
  );
};
