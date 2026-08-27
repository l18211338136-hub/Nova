import { useState, useEffect } from 'react'
import { OrganizationDto, OrganizationTreeDto } from '@/api/model'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { DictSelect } from '@/components/dict/dict-select'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'

type OrgDto = NonNullable<OrganizationDto>

interface OrganizationModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  initialData?: Partial<OrgDto> | null
  parentNode?: OrganizationTreeDto | null
  allTreeData: OrganizationTreeDto[]
  onSave: (data: Partial<OrgDto>) => Promise<void>
}

export function OrganizationModal({
  open,
  onOpenChange,
  initialData,
  parentNode,
  allTreeData,
  onSave,
}: OrganizationModalProps) {
  const [formData, setFormData] = useState<Partial<OrgDto>>({
    name: '',
    code: '',
    type: 'department',
    parentId: null,
    sort: 0,
    phone: '',
    email: '',
    remarks: '',
  })
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    if (open) {
      if (initialData) {
        setFormData(initialData)
      } else if (parentNode) {
        setFormData({
          name: '',
          code: '',
          type: 'department',
          parentId: parentNode.id,
          sort: 0,
          phone: '',
          email: '',
          remarks: '',
        })
      } else {
        setFormData({
          name: '',
          code: '',
          type: 'group',
          parentId: null,
          sort: 0,
          phone: '',
          email: '',
          remarks: '',
        })
      }
    }
  }, [open, initialData, parentNode])

  const flattenNodes = (nodes: OrganizationTreeDto[], level = 0): { id: string; name: string; level: number }[] => {
    let result: { id: string; name: string; level: number }[] = []
    for (const n of nodes) {
      if (n.id && n.name) {
        result.push({ id: n.id, name: n.name, level })
      }
      if (n.children && n.children.length > 0) {
        result = result.concat(flattenNodes(n.children, level + 1))
      }
    }
    return result
  }

  const parentOptions = flattenNodes(allTreeData)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setLoading(true)
    try {
      await onSave(formData)
      onOpenChange(false)
    } finally {
      setLoading(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[500px]">
        <DialogHeader>
          <DialogTitle>{initialData?.id ? '编辑组织机构' : '新建组织机构'}</DialogTitle>
          <DialogDescription className="text-xs">
            {parentNode ? `在【${parentNode.name}】下方新增下级子机构` : '填写机构详细信息。'}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4 py-2">
          <div className="space-y-1.5">
            <Label className="text-xs font-semibold">上级机构</Label>
            <select
              value={formData.parentId || ''}
              onChange={(e) => setFormData({ ...formData, parentId: e.target.value || null })}
              className="w-full h-8 px-2 rounded-md border text-xs bg-background focus:outline-none focus:ring-1 focus:ring-primary"
            >
              <option value="">(无上级 / 顶级机构)</option>
              {parentOptions.map((opt) => (
                <option key={opt.id} value={opt.id}>
                  {'— '.repeat(opt.level) + opt.name}
                </option>
              ))}
            </select>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <Label className="text-xs font-semibold">机构名称 *</Label>
              <Input
                value={formData.name || ''}
                onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                placeholder="请输入机构名称"
                className="h-8 text-xs"
                required
              />
            </div>

            <div className="space-y-1.5">
              <Label className="text-xs font-semibold">机构编码</Label>
              <Input
                value={formData.code || ''}
                onChange={(e) => setFormData({ ...formData, code: e.target.value })}
                placeholder="请输入机构编码"
                className="h-8 text-xs"
              />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <Label className="text-xs font-semibold">机构类型</Label>
              <DictSelect
                code="sys_org_type"
                value={formData.type || 'department'}
                onValueChange={(val) => setFormData({ ...formData, type: val })}
                className="h-8 text-xs"
              />
            </div>

            <div className="space-y-1.5">
              <Label className="text-xs font-semibold">排序号</Label>
              <Input
                type="number"
                value={formData.sort ?? 0}
                onChange={(e) => setFormData({ ...formData, sort: parseInt(e.target.value) || 0 })}
                className="h-8 text-xs"
              />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <Label className="text-xs font-semibold">联系电话</Label>
              <Input
                value={formData.phone || ''}
                onChange={(e) => setFormData({ ...formData, phone: e.target.value })}
                placeholder="请输入联系电话"
                className="h-8 text-xs"
              />
            </div>

            <div className="space-y-1.5">
              <Label className="text-xs font-semibold">联系邮箱</Label>
              <Input
                type="email"
                value={formData.email || ''}
                onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                placeholder="请输入联系邮箱"
                className="h-8 text-xs"
              />
            </div>
          </div>

          <DialogFooter className="pt-3">
            <Button type="button" variant="outline" size="sm" className="h-8 text-xs" onClick={() => onOpenChange(false)}>
              取消
            </Button>
            <Button type="submit" size="sm" disabled={loading} className="h-8 text-xs">
              {loading ? '提交中...' : '保存'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
