import { useState, useEffect } from 'react'
import { Plus, Trash2, Shield, EyeOff, Lock, Code } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Checkbox } from '@/components/ui/checkbox'

export interface EntityFieldMetadata {
  name: string
  displayName: string
  type: string
  supportMasking?: boolean
}

export interface EntityMetadata {
  entityName: string
  displayName: string
  fields: EntityFieldMetadata[]
}

export interface AbacCondition {
  field: string
  operator: string
  value: string
}

export interface AbacPolicyItem {
  targetEntity: string
  entityDisplayName?: string
  rowRules: {
    logic: 'AND' | 'OR'
    conditions: AbacCondition[]
  }
  fieldPermissions: {
    hiddenFields: string[]
    maskedFields: string[]
  }
}

interface AbacRuleBuilderProps {
  metadata: EntityMetadata[]
  policies: AbacPolicyItem[]
  onChange: (policies: AbacPolicyItem[]) => void
}

const OPERATORS = [
  { value: '=', label: '等于 (=)' },
  { value: '!=', label: '不等于 (!=)' },
  { value: '>', label: '大于 (>)' },
  { value: '>=', label: '大于等于 (>=)' },
  { value: '<', label: '小于 (<)' },
  { value: '<=', label: '小于等于 (<=)' },
  { value: 'Between', label: '介于...之间 (Between)' },
  { value: 'In', label: '包含于多值/枚举 (In)' },
  { value: 'Like', label: '模糊匹配 (Like)' },
]

const SYSTEM_VARIABLES = [
  { value: '@CurrentUserId', label: '👤 @CurrentUserId (当前登录用户)' },
  { value: '@CurrentOrgId', label: '🏢 @CurrentOrgId (当前所属部门)' },
  { value: '@CurrentOrgAndSubIds', label: '🌲 @CurrentOrgAndSubIds (当前部门及所有子级部门)' },
  { value: '@Today', label: '📅 @Today (今天)' },
  { value: '@Recent7Days', label: '🗓️ @Recent7Days (最近 7 天)' },
  { value: '@Recent30Days', label: '🗓️ @Recent30Days (最近 30 天)' },
  { value: '@ThisMonth', label: '📅 @ThisMonth (本月)' },
  { value: '@ThisYear', label: '📅 @ThisYear (今年)' },
]

export function AbacRuleBuilder({ metadata, policies, onChange }: AbacRuleBuilderProps) {
  const [selectedEntity, setSelectedEntity] = useState<string>('')

  useEffect(() => {
    if (metadata.length > 0 && !selectedEntity) {
      setSelectedEntity(metadata[0].entityName)
    }
  }, [metadata, selectedEntity])

  const currentMetadata = metadata.find((m) => m.entityName === selectedEntity)

  // Find or create policy for current entity
  const currentPolicy: AbacPolicyItem = policies.find((p) => p.targetEntity === selectedEntity) || {
    targetEntity: selectedEntity,
    entityDisplayName: currentMetadata?.displayName,
    rowRules: { logic: 'AND', conditions: [] },
    fieldPermissions: { hiddenFields: [], maskedFields: [] },
  }

  const updateCurrentPolicy = (newPolicy: AbacPolicyItem) => {
    const nextPolicies = policies.filter((p) => p.targetEntity !== selectedEntity)
    nextPolicies.push(newPolicy)
    onChange(nextPolicies)
  }

  const handleAddCondition = () => {
    if (!currentMetadata || currentMetadata.fields.length === 0) return
    const firstField = currentMetadata.fields[0].name
    const updatedConditions = [
      ...currentPolicy.rowRules.conditions,
      { field: firstField, operator: '=', value: '@CurrentOrgAndSubIds' },
    ]
    updateCurrentPolicy({
      ...currentPolicy,
      rowRules: { ...currentPolicy.rowRules, conditions: updatedConditions },
    })
  }

  const handleRemoveCondition = (index: number) => {
    const updatedConditions = currentPolicy.rowRules.conditions.filter((_, i) => i !== index)
    updateCurrentPolicy({
      ...currentPolicy,
      rowRules: { ...currentPolicy.rowRules, conditions: updatedConditions },
    })
  }

  const handleConditionChange = (index: number, key: keyof AbacCondition, val: string) => {
    const updatedConditions = [...currentPolicy.rowRules.conditions]
    updatedConditions[index] = { ...updatedConditions[index], [key]: val }
    updateCurrentPolicy({
      ...currentPolicy,
      rowRules: { ...currentPolicy.rowRules, conditions: updatedConditions },
    })
  }

  const handleToggleHiddenField = (fieldName: string, checked: boolean) => {
    const hidden = new Set(currentPolicy.fieldPermissions.hiddenFields)
    if (checked) hidden.add(fieldName)
    else hidden.delete(fieldName)

    updateCurrentPolicy({
      ...currentPolicy,
      fieldPermissions: {
        ...currentPolicy.fieldPermissions,
        hiddenFields: Array.from(hidden),
      },
    })
  }

  const handleToggleMaskedField = (fieldName: string, checked: boolean) => {
    const masked = new Set(currentPolicy.fieldPermissions.maskedFields)
    if (checked) masked.add(fieldName)
    else masked.delete(fieldName)

    updateCurrentPolicy({
      ...currentPolicy,
      fieldPermissions: {
        ...currentPolicy.fieldPermissions,
        maskedFields: Array.from(masked),
      },
    })
  }

  if (metadata.length === 0) {
    return (
      <div className="p-6 text-center text-xs text-muted-foreground border rounded-lg bg-muted/20">
        正在扫描系统 C# 反射属性元数据，请稍候...
      </div>
    )
  }

  return (
    <div className="space-y-4">
      {/* 1. Target Entity Select */}
      <div className="flex items-center gap-3 p-3 bg-muted/30 border rounded-lg">
        <Shield className="h-4 w-4 text-primary shrink-0" />
        <div className="flex-1 flex items-center gap-2">
          <Label className="text-xs font-semibold whitespace-nowrap">目标业务实体 (Resource Entity):</Label>
          <select
            value={selectedEntity}
            onChange={(e) => setSelectedEntity(e.target.value)}
            className="flex-1 max-w-sm h-8 px-2.5 rounded-md border text-xs bg-background focus:outline-none focus:ring-1 focus:ring-primary"
          >
            {metadata.map((item) => (
              <option key={item.entityName} value={item.entityName}>
                {item.displayName} ({item.entityName})
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* 2. Row Level Rule Builder */}
      <div className="p-4 border rounded-lg space-y-3 bg-card">
        <div className="flex items-center justify-between border-b pb-2">
          <div className="flex items-center gap-2">
            <Code className="h-3.5 w-3.5 text-primary" />
            <span className="text-xs font-bold">行级动态过滤条件 (Row-Level Expression Rules)</span>
          </div>
          <div className="flex items-center gap-2">
            <span className="text-xs text-muted-foreground">条件组逻辑:</span>
            <select
              value={currentPolicy.rowRules.logic}
              onChange={(e) =>
                updateCurrentPolicy({
                  ...currentPolicy,
                  rowRules: { ...currentPolicy.rowRules, logic: e.target.value as 'AND' | 'OR' },
                })
              }
              className="h-7 text-xs px-2 rounded border bg-background"
            >
              <option value="AND">满足所有条件 (AND)</option>
              <option value="OR">满足任一条件 (OR)</option>
            </select>
            <Button size="sm" variant="outline" className="h-7 text-xs gap-1" onClick={handleAddCondition}>
              <Plus className="h-3 w-3" />
              添加字段条件
            </Button>
          </div>
        </div>

        {currentPolicy.rowRules.conditions.length === 0 ? (
          <div className="py-4 text-center text-xs text-muted-foreground">
            暂未添加特定字段过滤规则（将仅受默认 DataScope 约束）。点击右上角“添加字段条件”配置表达式。
          </div>
        ) : (
          <div className="space-y-2">
            {currentPolicy.rowRules.conditions.map((cond, idx) => (
              <div key={idx} className="flex items-center gap-2 p-2 rounded border bg-muted/20 text-xs">
                {/* Field Select */}
                <select
                  value={cond.field}
                  onChange={(e) => handleConditionChange(idx, 'field', e.target.value)}
                  className="h-8 px-2 rounded border bg-background font-medium"
                >
                  {currentMetadata?.fields.map((f) => (
                    <option key={f.name} value={f.name}>
                      {f.displayName} [{f.name}]
                    </option>
                  ))}
                </select>

                {/* Operator Select */}
                <select
                  value={cond.operator}
                  onChange={(e) => handleConditionChange(idx, 'operator', e.target.value)}
                  className="h-8 px-2 rounded border bg-background font-medium"
                >
                  {OPERATORS.map((op) => (
                    <option key={op.value} value={op.value}>
                      {op.label}
                    </option>
                  ))}
                </select>

                {/* Value Input or Variable */}
                {cond.operator === 'Between' ? (
                  <div className="flex-1 flex items-center gap-1.5">
                    <Input
                      value={cond.value.split(',')[0] || ''}
                      onChange={(e) => {
                        const parts = cond.value.split(',')
                        const minVal = e.target.value
                        const maxVal = parts[1]?.trim() || ''
                        handleConditionChange(idx, 'value', `${minVal}, ${maxVal}`)
                      }}
                      placeholder="最小值 (如 20)"
                      className="h-8 text-xs flex-1 font-mono"
                    />
                    <span className="text-muted-foreground text-xs shrink-0">至</span>
                    <Input
                      value={cond.value.split(',')[1]?.trim() || ''}
                      onChange={(e) => {
                        const parts = cond.value.split(',')
                        const minVal = parts[0]?.trim() || ''
                        const maxVal = e.target.value
                        handleConditionChange(idx, 'value', `${minVal}, ${maxVal}`)
                      }}
                      placeholder="最大值 (如 30)"
                      className="h-8 text-xs flex-1 font-mono"
                    />
                  </div>
                ) : (
                  <div className="flex-1 flex items-center gap-1.5">
                    <Input
                      value={cond.value}
                      onChange={(e) => handleConditionChange(idx, 'value', e.target.value)}
                      placeholder={
                        cond.operator === 'In'
                          ? '多固定值逗号分隔 (如: 20, 30, 40 或 VIP, SVIP)'
                          : '手写固定值(如 100000, Approved, 2026-01-01) 或选择右侧变量'
                      }
                      className="h-8 text-xs flex-1 font-mono"
                    />
                    <select
                      value={SYSTEM_VARIABLES.some((v) => v.value === cond.value) ? cond.value : ''}
                      onChange={(e) => e.target.value && handleConditionChange(idx, 'value', e.target.value)}
                      className="h-8 px-2 rounded border bg-background text-[11px] max-w-[210px]"
                    >
                      <option value="">(快速选择内置/时间宏)</option>
                      {SYSTEM_VARIABLES.map((v) => (
                        <option key={v.value} value={v.value}>
                          {v.label}
                        </option>
                      ))}
                    </select>
                  </div>
                )}

                <Button
                  size="icon"
                  variant="ghost"
                  className="h-7 w-7 text-destructive hover:bg-destructive/10 shrink-0"
                  onClick={() => handleRemoveCondition(idx)}
                >
                  <Trash2 className="h-3.5 w-3.5" />
                </Button>
              </div>
            ))}

            {/* Quick Reference Tip */}
            <div className="mt-2 p-2.5 rounded bg-muted/40 text-[11px] text-muted-foreground space-y-1">
              <div className="font-semibold text-foreground flex items-center gap-1">
                💡 表达式配置写法示例：
              </div>
              <div className="grid grid-cols-2 gap-x-4 gap-y-1 font-mono">
                <div>• <span className="text-primary font-bold">数值/多固定值</span>：状态 [Status] In <span className="underline">20, 30, 40</span></div>
                <div>• <span className="text-primary font-bold">范围区间 (Between)</span>：年龄 [Age] 介于 <span className="underline">20</span> 至 <span className="underline">30</span></div>
                <div>• <span className="text-primary font-bold">固定日期区间</span>：时间 [CreatedAt] 介于 <span className="underline">2026-01-01</span> 至 <span className="underline">2026-12-31</span></div>
                <div>• <span className="text-primary font-bold">时间宏</span>：创建时间 [CreatedAt] &gt;= <span className="underline">@Recent30Days</span></div>
                <div>• <span className="text-primary font-bold">动态用户</span>：创建人 [CreatedBy] = <span className="underline">@CurrentUserId</span></div>
                <div>• <span className="text-primary font-bold">部门树</span>：归属部门 [OrganizationId] In <span className="underline">@CurrentOrgAndSubIds</span></div>
              </div>
            </div>
          </div>
        )}
      </div>

      {/* 3. Column/Field Level Permissions & Masking */}
      <div className="p-4 border rounded-lg space-y-3 bg-card">
        <div className="flex items-center gap-2 border-b pb-2">
          <EyeOff className="h-3.5 w-3.5 text-primary" />
          <span className="text-xs font-bold">字段级列访问与脱敏控制 (Column/Field-Level Permissions)</span>
        </div>

        {currentMetadata && currentMetadata.fields.length > 0 ? (
          <div className="grid grid-cols-2 gap-3">
            {currentMetadata.fields.map((field) => {
              const isHidden = currentPolicy.fieldPermissions.hiddenFields.includes(field.name)
              const isMasked = currentPolicy.fieldPermissions.maskedFields.includes(field.name)

              return (
                <div
                  key={field.name}
                  className="flex items-center justify-between p-2 rounded border bg-muted/10 text-xs"
                >
                  <div className="space-y-0.5">
                    <div className="font-semibold text-xs">{field.displayName}</div>
                    <div className="text-[10px] text-muted-foreground font-mono">
                      {field.name} ({field.type})
                    </div>
                  </div>

                  <div className="flex items-center gap-3">
                    <label className="flex items-center gap-1 cursor-pointer">
                      <Checkbox
                        checked={isHidden}
                        onCheckedChange={(checked) => handleToggleHiddenField(field.name, !!checked)}
                      />
                      <span className="text-[11px] text-muted-foreground flex items-center gap-0.5">
                        <EyeOff className="h-3 w-3" />
                        隐藏此列
                      </span>
                    </label>

                    {field.supportMasking !== false && (
                      <label className="flex items-center gap-1 cursor-pointer">
                        <Checkbox
                          checked={isMasked}
                          onCheckedChange={(checked) => handleToggleMaskedField(field.name, !!checked)}
                        />
                        <span className="text-[11px] text-muted-foreground flex items-center gap-0.5">
                          <Lock className="h-3 w-3" />
                          打码脱敏
                        </span>
                      </label>
                    )}
                  </div>
                </div>
              )
            })}
          </div>
        ) : (
          <div className="py-2 text-center text-xs text-muted-foreground">当前实体无独立配置列</div>
        )}
      </div>
    </div>
  )
}
