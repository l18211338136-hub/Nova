import { useState } from 'react'
import { Building, Save, Trash2 } from 'lucide-react'
import { toast } from 'sonner'
import { OrganizationDto } from '@/api/model'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Switch } from '@/components/ui/switch'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { DictSelect } from '@/components/dict/dict-select'

type OrgDto = NonNullable<OrganizationDto>

interface OrganizationSettingsTabProps {
  organization: OrgDto
  onSave: (updated: Partial<OrgDto>) => void
  onDelete: (id: string) => void
}

export function OrganizationSettingsTab({
  organization,
  onSave,
  onDelete,
}: OrganizationSettingsTabProps) {
  const [formData, setFormData] = useState<Partial<OrgDto>>({
    name: organization.name || '',
    code: organization.code || '',
    type: organization.type || 'department',
    sort: organization.sort ?? 0,
    phone: organization.phone || '',
    email: organization.email || '',
    isEnabled: organization.isEnabled ?? true,
    remarks: organization.remarks || '',
  })
  const [saving, setSaving] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setSaving(true)
    try {
      await onSave(formData)
      toast.success('机构基本信息保存成功')
    } catch (err: any) {
      toast.error('保存失败', { description: err?.response?.data?.title || err.message })
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base flex items-center gap-2">
            <Building className="h-4 w-4 text-primary" />
            机构基本信息
          </CardTitle>
          <CardDescription className="text-xs">
            修改当前机构的名称、类型、联系方式及状态设置。
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
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

            <div className="grid grid-cols-2 gap-4">
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

            <div className="grid grid-cols-2 gap-4">
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

            <div className="space-y-1.5">
              <Label className="text-xs font-semibold">备注说明</Label>
              <Input
                value={formData.remarks || ''}
                onChange={(e) => setFormData({ ...formData, remarks: e.target.value })}
                placeholder="请输入备注说明"
                className="h-8 text-xs"
              />
            </div>

            <div className="flex items-center justify-between pt-2 border-t">
              <div className="flex items-center gap-2">
                <Switch
                  checked={formData.isEnabled ?? true}
                  onCheckedChange={(checked) => setFormData({ ...formData, isEnabled: checked })}
                />
                <Label className="text-xs font-normal">启用此机构</Label>
              </div>

              <div className="flex items-center gap-2">
                <Button
                  type="button"
                  variant="destructive"
                  size="sm"
                  className="h-8 text-xs gap-1"
                  onClick={() => organization.id && onDelete(organization.id)}
                >
                  <Trash2 className="h-3.5 w-3.5" />
                  删除机构
                </Button>
                <Button type="submit" size="sm" disabled={saving} className="h-8 text-xs gap-1">
                  <Save className="h-3.5 w-3.5" />
                  {saving ? '保存中...' : '保存更改'}
                </Button>
              </div>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}
