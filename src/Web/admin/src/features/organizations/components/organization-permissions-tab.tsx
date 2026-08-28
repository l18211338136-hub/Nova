import { useState, useEffect } from 'react'
import { Check, ShieldCheck, Database } from 'lucide-react'
import { toast } from 'sonner'
import { OrganizationPermissionsDto } from '@/api/model'
import { apiClient } from '@/lib/api-client'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Label } from '@/components/ui/label'
import { AbacRuleBuilder, EntityMetadata, AbacPolicyItem } from './abac-rule-builder'

type OrgPermissionsDto = NonNullable<OrganizationPermissionsDto>

interface OrganizationPermissionsTabProps {
  permissions: OrgPermissionsDto
  onSave: (updated: OrgPermissionsDto) => void
}

const DATA_SCOPES = [
  { value: 1, label: '全部数据权限', desc: '拥有整个系统/所有部门的所有数据可见与操作权限' },
  { value: 2, label: '本部门数据权限', desc: '仅限制为当前所属部门及其直属数据' },
  { value: 3, label: '本部门及下级部门权限', desc: '包含当前部门及树状结构下方所有子机构的数据' },
  { value: 4, label: '仅本人数据权限', desc: '仅能查阅和维护当前登录用户自身创建的数据' },
]

// Fallback metadata if API is loading or offline
const DEFAULT_METADATA: EntityMetadata[] = [
  {
    entityName: 'Organization',
    displayName: '组织机构表 (Organization)',
    fields: [
      { name: 'Name', displayName: '机构名称', type: 'string' },
      { name: 'Code', displayName: '机构编码', type: 'string' },
      { name: 'Type', displayName: '机构类型', type: 'string' },
      { name: 'Level', displayName: '机构层级', type: 'number' },
      { name: 'Phone', displayName: '联系电话', type: 'string', supportMasking: true },
      { name: 'Email', displayName: '联系邮箱', type: 'string', supportMasking: true },
    ],
  },
  {
    entityName: 'Order',
    displayName: '订单业务表 (Order)',
    fields: [
      { name: 'Amount', displayName: '订单金额', type: 'number' },
      { name: 'Status', displayName: '订单状态', type: 'string' },
      { name: 'OrganizationId', displayName: '归属部门', type: 'string' },
      { name: 'CostPrice', displayName: '成本价格', type: 'number' },
      { name: 'CustomerPhone', displayName: '客户电话', type: 'string', supportMasking: true },
    ],
  },
]

export function OrganizationPermissionsTab({ permissions, onSave }: OrganizationPermissionsTabProps) {
  const [dataScope, setDataScope] = useState<number>(permissions.dataScope || 2)
  const [metadata, setMetadata] = useState<EntityMetadata[]>(DEFAULT_METADATA)
  const [policies, setPolicies] = useState<AbacPolicyItem[]>([])
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    // Parse existing abacPoliciesJson if available
    if (permissions.abacPoliciesJson) {
      try {
        const parsed = JSON.parse(permissions.abacPoliciesJson)
        if (Array.isArray(parsed)) {
          // Check if it's the old frontend format or the new backend format
          const isBackendFormat = parsed.length > 0 && ('entityName' in parsed[0] || 'EntityName' in parsed[0])
          
          if (isBackendFormat) {
            const frontendPolicies: AbacPolicyItem[] = parsed.map((p: any) => ({
              targetEntity: p.entityName || p.EntityName,
              rowRules: {
                logic: p.logic || p.Logic || 'AND',
                conditions: p.rules || p.Rules || []
              },
              fieldPermissions: {
                hiddenFields: (p.fields || p.Fields || []).filter((f: any) => f.hide || f.Hide).map((f: any) => f.field || f.Field),
                maskedFields: (p.fields || p.Fields || []).filter((f: any) => f.mask || f.Mask).map((f: any) => f.field || f.Field)
              }
            }))
            setPolicies(frontendPolicies)
          } else {
            // Legacy fallback
            setPolicies(parsed)
          }
        }
      } catch {
        // ignore parse error
      }
    }

    // Fetch dynamic C# reflection metadata
    const fetchMetadata = async () => {
      try {
        const res = await apiClient.get('/api/organizations/metadata/entities')
        const data = res.data?.data || res.data
        if (Array.isArray(data) && data.length > 0) {
          setMetadata(data)
        }
      } catch {
        // fallback to DEFAULT_METADATA
      }
    }

    fetchMetadata()
  }, [permissions])

  const handleSave = async () => {
    setSaving(true)
    try {
      // Map frontend AbacPolicyItem to backend AbacPolicyConfig structure
      const backendPolicies = policies.map((p) => {
        const fieldNames = new Set([...p.fieldPermissions.hiddenFields, ...p.fieldPermissions.maskedFields])
        const fields = Array.from(fieldNames).map((fieldName) => ({
          field: fieldName,
          hide: p.fieldPermissions.hiddenFields.includes(fieldName),
          mask: p.fieldPermissions.maskedFields.includes(fieldName)
        }))

        return {
          entityName: p.targetEntity,
          logic: p.rowRules.logic,
          rules: p.rowRules.conditions,
          fields: fields
        }
      })

      const policiesJson = JSON.stringify(backendPolicies)
      await onSave({
        ...permissions,
        dataScope,
        abacPoliciesJson: policiesJson,
      })
      toast.success('ABAC 权限策略配置保存成功')
    } catch (err: any) {
      toast.error('保存权限配置失败', { description: err?.response?.data?.title || err.message })
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="space-y-6 max-h-[calc(100vh-340px)] overflow-y-auto pr-2">
      {/* 1. Global Data Scope Option */}
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base flex items-center gap-2">
            <ShieldCheck className="h-4 w-4 text-primary" />
            基础数据范围隔离级别 (Data Scope)
          </CardTitle>
          <CardDescription className="text-xs">
            设置绑定该部门的用户在业务模块中默认的基础数据隔离与查看范围。
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-3">
          <div className="grid grid-cols-2 gap-3">
            {DATA_SCOPES.map((scope) => (
              <div
                key={scope.value}
                onClick={() => setDataScope(scope.value)}
                className={`flex items-start gap-3 p-3 rounded-lg border cursor-pointer transition-colors ${
                  dataScope === scope.value
                    ? 'border-primary bg-primary/5 text-primary'
                    : 'hover:bg-accent hover:border-accent-foreground/30'
                }`}
              >
                <div
                  className={`mt-0.5 h-4 w-4 rounded-full border flex items-center justify-center shrink-0 ${
                    dataScope === scope.value
                      ? 'border-primary bg-primary text-primary-foreground'
                      : 'border-muted-foreground'
                  }`}
                >
                  {dataScope === scope.value && <Check className="h-3 w-3 stroke-[3]" />}
                </div>
                <div className="space-y-0.5">
                  <Label className="font-semibold text-xs cursor-pointer">{scope.label}</Label>
                  <p className="text-[11px] text-muted-foreground">{scope.desc}</p>
                </div>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>

      {/* 2. ABAC Dynamic Rule & Field Permission Builder */}
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base flex items-center gap-2">
            <Database className="h-4 w-4 text-primary" />
            多实体 ABAC 动态表达式规则与字段列防护配置
          </CardTitle>
          <CardDescription className="text-xs">
            基于 C# 特性全自动反射的实体字段，配置按表划分的行级过滤表达式及列级隐匿/打码脱敏策略。
          </CardDescription>
        </CardHeader>
        <CardContent>
          <AbacRuleBuilder metadata={metadata} policies={policies} onChange={setPolicies} />

          <div className="pt-4 flex justify-end">
            <Button size="sm" onClick={handleSave} disabled={saving} className="text-xs">
              {saving ? '保存中...' : '保存 ABAC 权限策略配置'}
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
