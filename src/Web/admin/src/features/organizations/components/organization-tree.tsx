import { useState } from 'react'
import { Building2, ChevronDown, ChevronRight, FolderTree, Plus, Search, Users } from 'lucide-react'
import { OrganizationTreeDto } from '@/api/model'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'

interface OrganizationTreeProps {
  treeData: OrganizationTreeDto[]
  selectedId: string | null
  onSelect: (node: OrganizationTreeDto) => void
  onAddSub: (parentNode: OrganizationTreeDto) => void
  onAddRoot: () => void
}

export function OrganizationTree({
  treeData,
  selectedId,
  onSelect,
  onAddSub,
  onAddRoot,
}: OrganizationTreeProps) {
  const [filterText, setFilterText] = useState('')
  const [expandedIds, setExpandedIds] = useState<Record<string, boolean>>({})

  const toggleExpand = (id: string, e: React.MouseEvent) => {
    e.stopPropagation()
    setExpandedIds((prev) => ({ ...prev, [id]: !prev[id] }))
  }

  const matchesFilter = (node: OrganizationTreeDto): boolean => {
    if (!filterText.trim()) return true
    const term = filterText.toLowerCase()
    if ((node.name || '').toLowerCase().includes(term) || node.code?.toLowerCase().includes(term)) {
      return true
    }
    return (node.children || []).some(matchesFilter)
  }

  const renderTreeNode = (node: OrganizationTreeDto, depth: number = 0) => {
    if (!matchesFilter(node)) return null

    const hasChildren = !!(node.children && node.children.length > 0)
    const isExpanded = node.id ? (expandedIds[node.id] ?? true) : true
    const isSelected = !!node.id && selectedId === node.id

    return (
      <div key={node.id || Math.random()} className="select-none">
        <div
          onClick={() => onSelect(node)}
          style={{ paddingLeft: `${depth * 16 + 8}px` }}
          className={`group flex items-center justify-between rounded-md py-1.5 pr-2 text-sm cursor-pointer transition-colors ${
            isSelected
              ? 'bg-primary/15 text-primary font-medium'
              : 'hover:bg-accent hover:text-accent-foreground text-muted-foreground'
          }`}
        >
          <div className="flex items-center gap-1.5 min-w-0 overflow-hidden">
            {hasChildren ? (
              <button
                type="button"
                onClick={(e) => node.id && toggleExpand(node.id, e)}
                className="p-0.5 rounded hover:bg-muted/80 text-muted-foreground"
              >
                {isExpanded ? (
                  <ChevronDown className="h-3.5 w-3.5" />
                ) : (
                  <ChevronRight className="h-3.5 w-3.5" />
                )}
              </button>
            ) : (
              <span className="w-4" />
            )}

            <Building2 className={`h-4 w-4 shrink-0 ${isSelected ? 'text-primary' : 'text-muted-foreground'}`} />
            <span className="truncate">{node.name || '未命名'}</span>

            {node.type && (
              <Badge variant="outline" className="text-[10px] px-1 py-0 h-4 uppercase font-normal">
                {node.type}
              </Badge>
            )}

            <Badge variant="secondary" className="text-[10px] px-1 py-0 h-4 font-mono">
              L{node.level ?? 1}
            </Badge>
          </div>

          <div className="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
            <span className="flex items-center gap-0.5 text-xs text-muted-foreground mr-1">
              <Users className="h-3 w-3" />
              {node.memberCount ?? 0}
            </span>
            <Button
              size="icon"
              variant="ghost"
              className="h-6 w-6 p-0 hover:bg-background"
              title="添加下级机构"
              onClick={(e) => {
                e.stopPropagation()
                onAddSub(node)
              }}
            >
              <Plus className="h-3.5 w-3.5" />
            </Button>
          </div>
        </div>

        {hasChildren && isExpanded && (
          <div className="mt-0.5 space-y-0.5">
            {node.children?.map((child) => renderTreeNode(child, depth + 1))}
          </div>
        )}
      </div>
    )
  }

  return (
    <div className="flex flex-col h-full bg-card border rounded-lg p-3">
      <div className="flex items-center justify-between mb-3 pb-2 border-b">
        <div className="flex items-center gap-2 font-semibold text-sm">
          <FolderTree className="h-4 w-4 text-primary" />
          组织架构
        </div>
        <Button size="sm" variant="default" className="h-8 text-xs gap-1" onClick={onAddRoot}>
          <Plus className="h-3.5 w-3.5" />
          新增根节点
        </Button>
      </div>

      <div className="relative mb-3">
        <Search className="absolute left-2.5 top-2.5 h-3.5 w-3.5 text-muted-foreground" />
        <Input
          placeholder="搜索机构名称或编码..."
          value={filterText}
          onChange={(e) => setFilterText(e.target.value)}
          className="pl-8 h-8 text-xs"
        />
      </div>

      <div className="flex-1 overflow-y-auto pr-1 space-y-0.5">
        {treeData.length === 0 ? (
          <div className="py-8 text-center text-xs text-muted-foreground">暂无组织机构结构</div>
        ) : (
          treeData.map((node) => renderTreeNode(node, 0))
        )}
      </div>
    </div>
  )
}
