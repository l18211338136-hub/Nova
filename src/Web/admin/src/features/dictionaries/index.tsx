import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Header } from '@/components/layout/header'
import { Main } from '@/components/layout/main'
import { ProfileDropdown } from '@/components/profile-dropdown'
import { ConfigDrawer } from '@/components/config-drawer'
import { ThemeSwitch } from '@/components/theme-switch'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import {
  Plus,
  Edit,
  Trash2,
  Search,
  BookOpen,
  Tag,
  MoreVertical,
  CheckCircle2,
  XCircle,
} from 'lucide-react'
import { toast } from 'sonner'
import { useQueryClient } from '@tanstack/react-query'
import { ConfirmDialog } from '@/components/confirm-dialog'
import { DictionaryTypeDto, DictionaryItemDto } from '@/api/model'
import {
  useDictionaryTypes,
  useDictionaryItems,
  useDeleteDictionaryType,
  useDeleteDictionaryItem,
  useUpdateDictionaryItem,
  getDictionaryTypesQueryKey as getTypesQueryKey,
  getDictionaryItemsQueryKey as getItemsQueryKey,
} from '@/api/endpoints/dictionaries'
import { DictTypeDialog } from './dict-type-dialog'
import { DictItemDialog } from './dict-item-dialog'

export default function Dictionaries() {
  const { t } = useTranslation()
  const queryClient = useQueryClient()

  // State
  const [typeSearch, setTypeSearch] = useState('')
  const [itemSearch, setItemSearch] = useState('')
  const [selectedType, setSelectedType] = useState<DictionaryTypeDto | null>(null)

  // Dialog State
  const [typeDialogOpen, setTypeDialogOpen] = useState(false)
  const [editingType, setEditingType] = useState<DictionaryTypeDto | null>(null)

  const [itemDialogOpen, setItemDialogOpen] = useState(false)
  const [editingItem, setEditingItem] = useState<DictionaryItemDto | null>(null)

  // Delete Confirm State
  const [deletingType, setDeletingType] = useState<DictionaryTypeDto | null>(null)
  const [deletingItem, setDeletingItem] = useState<DictionaryItemDto | null>(null)

  // API Queries & Mutations
  const { data: typesData, isLoading: isTypesLoading, refetch: refetchTypes } = useDictionaryTypes()
  const rawTypesList = typesData?.data?.items || []

  // Filter dictionary types by left search box
  const typesList = rawTypesList.filter(
    (type) =>
      !typeSearch ||
      (type.name || '').toLowerCase().includes(typeSearch.toLowerCase()) ||
      (type.code || '').toLowerCase().includes(typeSearch.toLowerCase())
  )

  // Auto select first type if none selected
  const activeType = selectedType || (typesList.length > 0 ? typesList[0] : null)

  // Query dictionary items
  const { data: itemsData, isLoading: isItemsLoading, refetch: refetchItems } = useDictionaryItems()
  const rawItemsList = itemsData?.data?.items || []

  // Filter dictionary items by activeType and right search box
  const itemsList = rawItemsList.filter(
    (item) =>
      (activeType ? (item.typeId === activeType.id || item.typeCode === activeType.code) : true) &&
      (!itemSearch ||
        (item.label || '').toLowerCase().includes(itemSearch.toLowerCase()) ||
        (item.value || '').toLowerCase().includes(itemSearch.toLowerCase()))
  )

  const deleteTypeMutation = useDeleteDictionaryType()
  const deleteItemMutation = useDeleteDictionaryItem()
  const updateItemMutation = useUpdateDictionaryItem()

  // Handlers for Dict Types
  const handleCreateType = () => {
    setEditingType(null)
    setTypeDialogOpen(true)
  }

  const handleEditType = (type: DictionaryTypeDto) => {
    setEditingType(type)
    setTypeDialogOpen(true)
  }

  const handleDeleteType = (type: DictionaryTypeDto) => {
    if (type.isSystem) {
      toast.error(t('系统预置字典禁止删除'))
      return
    }
    setDeletingType(type)
  }

  const confirmDeleteType = async () => {
    if (!deletingType) return
    try {
      await deleteTypeMutation.mutateAsync({ id: deletingType.id! })
      toast.success(t('删除字典类型成功'))
      await queryClient.invalidateQueries({ queryKey: getTypesQueryKey() })
      await refetchTypes()
      if (selectedType?.id === deletingType.id) {
        setSelectedType(null)
      }
      setDeletingType(null)
    } catch (err: any) {
      toast.error(err?.message || t('删除失败'))
    }
  }

  // Handlers for Dict Items
  const handleCreateItem = () => {
    if (!activeType) {
      toast.error(t('请先选择一个字典类型'))
      return
    }
    setEditingItem(null)
    setItemDialogOpen(true)
  }

  const handleEditItem = (item: DictionaryItemDto) => {
    setEditingItem(item)
    setItemDialogOpen(true)
  }

  const handleDeleteItem = (item: DictionaryItemDto) => {
    setDeletingItem(item)
  }

  const confirmDeleteItem = async () => {
    if (!deletingItem) return
    try {
      await deleteItemMutation.mutateAsync({ id: deletingItem.id! })
      toast.success(t('删除字典数据项成功'))
      await queryClient.invalidateQueries({ queryKey: getItemsQueryKey() })
      await refetchItems()
      setDeletingItem(null)
    } catch (err: any) {
      toast.error(err?.message || t('删除失败'))
    }
  }

  const handleToggleItemStatus = async (item: DictionaryItemDto) => {
    try {
      await updateItemMutation.mutateAsync({
        id: item.id!,
        data: {
          label: item.label!,
          value: item.value!,
          tagType: item.tagType,
          sortOrder: item.sortOrder!,
          isDefault: item.isDefault!,
          isEnabled: !item.isEnabled,
          remarks: item.remarks,
        },
      })
      toast.success(t('更新状态成功'))
      await queryClient.invalidateQueries({ queryKey: getItemsQueryKey() })
      await refetchItems()
    } catch (err: any) {
      toast.error(err?.message || t('更新状态失败'))
    }
  }

  return (
    <>
      <Header fixed>
        <div className='flex items-center gap-2 me-auto font-medium text-sm text-muted-foreground'>
          <BookOpen className='h-4 w-4' />
          <span>{t('字典管理')}</span>
        </div>
        <ThemeSwitch />
        <ConfigDrawer />
        <ProfileDropdown />
      </Header>

      <Main className='flex flex-1 flex-col gap-4 sm:gap-6'>
        {/* Page Header */}
        <div className='flex flex-wrap items-center justify-between gap-2'>
          <div>
            <h2 className='text-2xl font-bold tracking-tight'>{t('数据字典管理')}</h2>
            <p className='text-muted-foreground text-sm'>
              {t('维护系统全局常用数据字典与明细数据项，供各模块拉取与展示。')}
            </p>
          </div>
          <Button onClick={handleCreateType}>
            <Plus className='mr-2 h-4 w-4' />
            {t('新增字典类型')}
          </Button>
        </div>

        {/* Master-Detail Split Grid */}
        <div className='grid grid-cols-1 md:grid-cols-12 gap-6 items-start'>
          {/* Left Column: Dictionary Types List (4 cols) */}
          <Card className='md:col-span-4 shadow-sm'>
            <CardHeader className='pb-3'>
              <CardTitle className='text-base font-semibold flex items-center justify-between'>
                <span>{t('字典分类')}</span>
                <Badge variant='outline'>{typesList.length}</Badge>
              </CardTitle>
              <div className='relative mt-2'>
                <Search className='absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground' />
                <Input
                  placeholder={t('搜索类型编码/名称...')}
                  className='pl-8 h-9'
                  value={typeSearch}
                  onChange={(e) => setTypeSearch(e.target.value)}
                />
              </div>
            </CardHeader>
            <CardContent className='p-0 max-h-[600px] overflow-y-auto divide-y'>
              {isTypesLoading ? (
                <div className='p-4 text-center text-sm text-muted-foreground'>{t('加载字典分类中...')}</div>
              ) : typesList.length === 0 ? (
                <div className='p-4 text-center text-sm text-muted-foreground'>{t('暂无字典分类')}</div>
              ) : (
                typesList.map((type) => {
                  const isSelected = activeType?.id === type.id
                  return (
                    <div
                      key={type.id}
                      onClick={() => setSelectedType(type)}
                      className={`flex items-center justify-between p-3.5 cursor-pointer transition-colors hover:bg-muted/50 ${isSelected ? 'bg-muted/80 font-medium border-l-4 border-primary pl-2.5' : ''
                        }`}
                    >
                      <div className='space-y-1 overflow-hidden pr-2'>
                        <div className='flex items-center gap-2'>
                          <span className='truncate text-sm font-semibold'>{type.name}</span>
                          {type.isSystem && (
                            <Badge variant='secondary' className='text-[10px] px-1 py-0'>
                              {t('系统')}
                            </Badge>
                          )}
                        </div>
                      </div>

                      <DropdownMenu>
                        <DropdownMenuTrigger asChild onClick={(e) => e.stopPropagation()}>
                          <Button variant='ghost' size='icon' className='h-8 w-8'>
                            <MoreVertical className='h-4 w-4' />
                          </Button>
                        </DropdownMenuTrigger>
                        <DropdownMenuContent align='end'>
                          <DropdownMenuItem onClick={() => handleEditType(type)}>
                            <Edit className='mr-2 h-4 w-4' />
                            {t('编辑')}
                          </DropdownMenuItem>
                          {!type.isSystem && (
                            <DropdownMenuItem
                              className='text-destructive'
                              onClick={() => handleDeleteType(type)}
                            >
                              <Trash2 className='mr-2 h-4 w-4' />
                              {t('删除')}
                            </DropdownMenuItem>
                          )}
                        </DropdownMenuContent>
                      </DropdownMenu>
                    </div>
                  )
                })
              )}
            </CardContent>
          </Card>

          {/* Right Column: Dictionary Items Details (8 cols) */}
          <Card className='md:col-span-8 shadow-sm'>
            <CardHeader className='pb-3 border-b'>
              <div className='flex flex-wrap items-center justify-between gap-2'>
                <div>
                  <CardTitle className='text-base font-semibold flex items-center gap-2'>
                    <Tag className='h-4 w-4 text-primary' />
                    <span>
                      {activeType ? activeType.name : t('请选择字典类型')}
                    </span>
                  </CardTitle>
                  {activeType?.description && (
                    <p className='text-xs text-muted-foreground mt-1'>{activeType.description}</p>
                  )}
                </div>
                {activeType && (
                  <Button size='sm' onClick={handleCreateItem}>
                    <Plus className='mr-1.5 h-3.5 w-3.5' />
                    {t('新增字典项')}
                  </Button>
                )}
              </div>
              <div className='relative mt-3'>
                <Search className='absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground' />
                <Input
                  placeholder={t('搜索字典数据项文本/键值...')}
                  className='pl-8 h-9'
                  value={itemSearch}
                  onChange={(e) => setItemSearch(e.target.value)}
                  disabled={!activeType}
                />
              </div>
            </CardHeader>
            <CardContent className='p-0'>
              {!activeType ? (
                <div className='p-8 text-center text-muted-foreground text-sm'>
                  {t('👈 请在左侧选择一个字典分类以管理其数据明细')}
                </div>
              ) : isItemsLoading ? (
                <div className='p-8 text-center text-muted-foreground text-sm'>
                  {t('正在加载字典数据项...')}
                </div>
              ) : itemsList.length === 0 ? (
                <div className='p-8 text-center text-muted-foreground text-sm space-y-2'>
                  <p>{t('该字典分类下暂无任何数据项')}</p>
                  <Button variant='outline' size='sm' onClick={handleCreateItem}>
                    <Plus className='mr-1 h-3.5 w-3.5' />
                    {t('添加第一个数据项')}
                  </Button>
                </div>
              ) : (
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>{t('显示文本')}</TableHead>
                      <TableHead>{t('数据值')}</TableHead>
                      <TableHead>{t('排序')}</TableHead>
                      <TableHead>{t('状态')}</TableHead>
                      <TableHead className='text-right'>{t('操作')}</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {itemsList.map((item) => (
                      <TableRow key={item.id}>
                        <TableCell className='font-medium'>
                          <div className='flex items-center gap-1.5'>
                            <span>{item.label}</span>
                            {item.isDefault && (
                              <Badge variant='outline' className='text-[10px] text-primary border-primary px-1 py-0'>
                                {t('默认')}
                              </Badge>
                            )}
                          </div>
                        </TableCell>
                        <TableCell className='font-mono text-sm'>{item.value}</TableCell>
                        <TableCell>{item.sortOrder}</TableCell>
                        <TableCell>
                          <Button
                            variant='ghost'
                            size='sm'
                            className='h-6 px-2 text-xs gap-1'
                            onClick={() => handleToggleItemStatus(item)}
                          >
                            {item.isEnabled ? (
                              <>
                                <CheckCircle2 className='h-3.5 w-3.5 text-emerald-500' />
                                <span className='text-emerald-600'>{t('启用')}</span>
                              </>
                            ) : (
                              <>
                                <XCircle className='h-3.5 w-3.5 text-muted-foreground' />
                                <span className='text-muted-foreground'>{t('禁用')}</span>
                              </>
                            )}
                          </Button>
                        </TableCell>
                        <TableCell className='text-right'>
                          <div className='flex justify-end gap-1'>
                            <Button
                              variant='ghost'
                              size='icon'
                              className='h-8 w-8'
                              onClick={() => handleEditItem(item)}
                            >
                              <Edit className='h-4 w-4' />
                            </Button>
                            <Button
                              variant='ghost'
                              size='icon'
                              className='h-8 w-8 text-destructive'
                              onClick={() => handleDeleteItem(item)}
                            >
                              <Trash2 className='h-4 w-4' />
                            </Button>
                          </div>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              )}
            </CardContent>
          </Card>
        </div>
      </Main>

      {/* Dialog Modals */}
      <DictTypeDialog
        open={typeDialogOpen}
        onOpenChange={setTypeDialogOpen}
        editingType={editingType}
      />

      {activeType && (
        <DictItemDialog
          open={itemDialogOpen}
          onOpenChange={setItemDialogOpen}
          typeId={activeType.id!}
          editingItem={editingItem}
        />
      )}

      {/* Confirm Delete Modals */}
      <ConfirmDialog
        open={!!deletingType}
        onOpenChange={(open) => !open && setDeletingType(null)}
        handleConfirm={confirmDeleteType}
        title={t('删除字典分类')}
        desc={t('确定要删除字典分类 "{{name}}" 及其所有数据项吗？此操作无法撤销。', { name: deletingType?.name })}
        confirmText={t('删除')}
        destructive
        isLoading={deleteTypeMutation.isPending}
      />

      <ConfirmDialog
        open={!!deletingItem}
        onOpenChange={(open) => !open && setDeletingItem(null)}
        handleConfirm={confirmDeleteItem}
        title={t('删除字典数据项')}
        desc={t('确定要删除字典数据项 "{{label}}" 吗？此操作无法撤销。', { label: deletingItem?.label })}
        confirmText={t('删除')}
        destructive
        isLoading={deleteItemMutation.isPending}
      />
    </>
  )
}
