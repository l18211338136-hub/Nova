import { useState, useEffect } from 'react'
import { Building, ShieldCheck, Users } from 'lucide-react'
import { toast } from 'sonner'
import {
  OrganizationDto,
  OrganizationTreeDto,
  OrganizationMemberDto,
  OrganizationPermissionsDto,
} from '@/api/model'
import {
  organizationTree,
  organizationById,
  createOrganization,
  updateOrganization,
  deleteOrganization,
  organizationMembers,
  organizationPermissions,
  saveOrganizationPermissions,
  updateOrganizationMembers,
} from '@/api/endpoints/organizations'
import { Badge } from '@/components/ui/badge'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { DictTag } from '@/components/dict/dict-tag'
import { Header } from '@/components/layout/header'
import { Main } from '@/components/layout/main'
import { ProfileDropdown } from '@/components/profile-dropdown'
import { Search } from '@/components/search'
import { ConfigDrawer } from '@/components/config-drawer'
import { ThemeSwitch } from '@/components/theme-switch'
import { ConfirmDialog } from '@/components/confirm-dialog'
import { OrganizationTree } from './components/organization-tree'
import { OrganizationMembersTable } from './components/organization-members-table'
import { OrganizationPermissionsTab } from './components/organization-permissions-tab'
import { OrganizationSettingsTab } from './components/organization-settings-tab'
import { OrganizationModal } from './components/organization-modal'
import { AddMemberModal, SelectedMemberCandidate } from './components/add-member-modal'

type OrgDto = NonNullable<OrganizationDto>
type OrgPermissionsDto = NonNullable<OrganizationPermissionsDto>

export default function OrganizationsFeature() {
  const [treeData, setTreeData] = useState<OrganizationTreeDto[]>([])
  const [selectedNode, setSelectedNode] = useState<OrganizationTreeDto | null>(null)
  const [selectedDetail, setSelectedDetail] = useState<OrgDto | null>(null)
  const [members, setMembers] = useState<OrganizationMemberDto[]>([])
  const [permissions, setPermissions] = useState<OrgPermissionsDto | null>(null)
  const [_loading, setLoading] = useState(true)

  // Modal State
  const [modalOpen, setModalOpen] = useState(false)
  const [modalParentNode, setModalParentNode] = useState<OrganizationTreeDto | null>(null)
  const [modalInitialData, setModalInitialData] = useState<Partial<OrgDto> | null>(null)
  const [addMemberModalOpen, setAddMemberModalOpen] = useState(false)
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false)
  const [nodeToDelete, setNodeToDelete] = useState<string | null>(null)

  const handleRemoveMember = async (userId: string) => {
    if (!selectedDetail?.id) return
    const updated = members.filter((m) => m.userId !== userId)
    setMembers(updated)
    try {
      await updateOrganizationMembers(selectedDetail.id, {
        id: selectedDetail.id,
        members: updated,
      })
      toast.success('已从部门移除该成员')
      loadTree()
    } catch {
      toast.error('保存成员变更失败')
    }
  }

  const handleConfirmAddMembers = async (newMembers: SelectedMemberCandidate[]) => {
    if (!selectedDetail?.id) return

    const newlyAdded: OrganizationMemberDto[] = newMembers.map((cand) => ({
      userId: cand.userId,
      organizationId: selectedDetail.id,
      userName: cand.userName,
      nickName: cand.nickName,
      email: cand.email,
      jobTitle: cand.jobTitle || '普通成员',
      isPrimary: cand.isPrimary,
      isLeader: false,
      joinedAt: new Date().toISOString(),
    }))

    const updated = [...members, ...newlyAdded]
    setMembers(updated)

    try {
      await updateOrganizationMembers(selectedDetail.id, {
        id: selectedDetail.id,
        members: updated,
      })
      toast.success(`成功添加 ${newlyAdded.length} 名部门成员`)
      loadTree()
    } catch {
      toast.error('保存成员列表失败')
    }
  }

  const loadTree = async () => {
    setLoading(true)
    try {
      const res = await organizationTree()
      const data = (res?.data as any)?.items || res?.data || []
      setTreeData(data)
      if (data.length > 0 && !selectedNode) {
        handleSelectNode(data[0])
      }
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadTree()
  }, [])

  const handleSelectNode = async (node: OrganizationTreeDto) => {
    setSelectedNode(node)
    try {
      const resDetail = await organizationById(node.id!)
      const detail = (resDetail?.data as any) || resDetail
      setSelectedDetail(detail)

      const resMem = await organizationMembers(node.id!)
      const memList = (resMem?.data as any)?.items || resMem?.data || []
      setMembers(memList)

      const resPerm = await organizationPermissions(node.id!)
      const permData = (resPerm?.data as any) || resPerm
      setPermissions(permData)
    } catch {
      // fallback Mock
      setSelectedDetail({
        id: node.id!,
        parentId: node.parentId,
        name: node.name!,
        code: node.code,
        type: node.type,
        level: node.level!,
        sort: node.sort!,
        leaderUserId: null,
        phone: '021-88888888',
        email: 'dept@nova.com',
        isEnabled: node.isEnabled,
        remarks: '模拟部门示例数据',
      })
      setMembers([
        {
          userId: 'usr-01',
          organizationId: node.id!,
          userName: '张三',
          nickName: '张三 (研发主管)',
          email: 'zhangsan@nova.com',
          isPrimary: true,
          jobTitle: '高级工程师',
          isLeader: true,
          joinedAt: new Date().toISOString(),
        },
        {
          userId: 'usr-02',
          organizationId: node.id!,
          userName: '李四',
          nickName: '李四',
          email: 'lisi@nova.com',
          isPrimary: false,
          jobTitle: '前端工程师',
          isLeader: false,
          joinedAt: new Date().toISOString(),
        },
      ])
      setPermissions({
        organizationId: node.id!,
        dataScope: 2,
        abacPoliciesJson: '[]',
      })
    }
  }

  const handleAddRoot = () => {
    setModalParentNode(null)
    setModalInitialData(null)
    setModalOpen(true)
  }

  const handleAddSub = (parentNode: OrganizationTreeDto) => {
    setModalParentNode(parentNode)
    setModalInitialData(null)
    setModalOpen(true)
  }

  const handleEditNode = async (node: OrganizationTreeDto) => {
    const parentNode = node.parentId ? (findNodeInTree(treeData, node.parentId) || null) : null
    setModalParentNode(parentNode)
    
    if (node.id) {
      try {
        const res = await organizationById(node.id)
        const detail = res.data
        if (detail) {
          setModalInitialData({
            id: detail.id,
            name: detail.name,
            parentId: detail.parentId,
            code: detail.code,
            type: detail.type,
            sort: detail.sort,
            isEnabled: detail.isEnabled,
            phone: detail.phone,
            email: detail.email,
            remarks: detail.remarks,
          })
          setModalOpen(true)
          return
        }
      } catch {
        toast.error('获取机构详情失败')
      }
    }

    // Fallback if no ID or API fails
    setModalInitialData({
      id: node.id,
      name: node.name,
      parentId: node.parentId,
      code: node.code,
      type: node.type,
      sort: node.sort,
      isEnabled: node.isEnabled,
    })
    setModalOpen(true)
  }

  const findNodeInTree = (nodes: OrganizationTreeDto[], targetId: string): OrganizationTreeDto | undefined => {
    for (const node of nodes) {
      if (node.id === targetId) return node
      if (node.children) {
        const found = findNodeInTree(node.children, targetId)
        if (found) return found
      }
    }
    return undefined
  }

  const handleSaveModal = async (data?: Partial<OrgDto> | null) => {
    if (!data) return
    try {
      if (data.id) {
        await updateOrganization(data.id, {
          id: data.id,
          name: data.name,
          parentId: data.parentId,
          code: data.code,
          type: data.type,
          sort: data.sort ?? 0,
          phone: data.phone,
          email: data.email,
          remarks: data.remarks,
          isEnabled: data.isEnabled,
        })
        toast.success('保存组织机构成功')
      } else {
        await createOrganization({
          name: data.name,
          parentId: data.parentId,
          code: data.code,
          type: data.type,
          sort: data.sort ?? 0,
          phone: data.phone,
          email: data.email,
          remarks: data.remarks,
        })
        toast.success('新建组织机构成功')
      }
      await loadTree()
    } catch (err: any) {
      toast.error('保存失败', { description: err?.response?.data?.title || err.message })
    }
  }

  const handleDelete = (id: string) => {
    setNodeToDelete(id)
    setDeleteConfirmOpen(true)
  }

  const handleConfirmDelete = async () => {
    if (!nodeToDelete) return
    try {
      await deleteOrganization(nodeToDelete)
      toast.success('删除组织机构成功')
      if (selectedNode?.id === nodeToDelete) {
        setSelectedNode(null)
        setSelectedDetail(null)
      }
      await loadTree()
    } catch (err: any) {
      toast.error('删除失败', { description: err?.response?.data?.title || err.message })
    } finally {
      setDeleteConfirmOpen(false)
      setNodeToDelete(null)
    }
  }

  return (
    <>
      <Header fixed>
        <Search className="me-auto" />
        <ThemeSwitch />
        <ConfigDrawer />
        <ProfileDropdown />
      </Header>

      <Main fixed className="flex flex-1 flex-col gap-4 sm:gap-6 overflow-hidden">
        <div className="flex flex-wrap items-end justify-between gap-2">
          <div>
            <h2 className="text-2xl font-bold tracking-tight">组织架构</h2>
            <p className="text-muted-foreground">
              树状多层级结构管理集团、公司、部门及团队，结合数据字典动态设定机构类型与数据权限。
            </p>
          </div>
        </div>

        <div className="grid grid-cols-12 gap-6 flex-1 min-h-0 overflow-hidden">
          {/* Left Side: Tree View */}
          <div className="col-span-4 h-full flex flex-col min-h-0 overflow-hidden">
            <OrganizationTree
              treeData={treeData}
              selectedId={selectedNode?.id || null}
              onSelect={handleSelectNode}
              onAddSub={handleAddSub}
              onAddRoot={handleAddRoot}
              onEdit={handleEditNode}
              onDelete={(node) => node.id && handleDelete(node.id)}
            />
          </div>

          {/* Right Side: Detail & Tabs */}
          <div className="col-span-8 h-full flex flex-col bg-card border rounded-lg p-5 min-h-0 overflow-hidden">
            {selectedDetail ? (
              <div className="space-y-4 flex-1 flex flex-col">
                <div className="flex items-center justify-between border-b pb-3">
                  <div className="space-y-0.5">
                    <div className="flex items-center gap-2">
                      <h2 className="text-lg font-bold">{selectedDetail.name}</h2>
                      {selectedDetail.type && (
                        <DictTag
                          code="sys_org_type"
                          value={selectedDetail.type}
                          fallbackLabel={selectedDetail.type}
                          className="text-xs font-normal"
                        />
                      )}
                      <Badge variant="secondary" className="text-xs">
                        层级: L{selectedDetail.level}
                      </Badge>
                    </div>
                    <p className="text-xs text-muted-foreground">
                      机构编码: {selectedDetail.code || '未设置'} | 成员数: {members.length} 人
                    </p>
                  </div>
                </div>

                <Tabs defaultValue="members" className="flex-1 flex flex-col">
                  <TabsList className="grid w-full grid-cols-3 max-w-md h-9">
                    <TabsTrigger value="members" className="text-xs gap-1.5">
                      <Users className="h-3.5 w-3.5" />
                      部门用户 ({members.length})
                    </TabsTrigger>
                    <TabsTrigger value="permissions" className="text-xs gap-1.5">
                      <ShieldCheck className="h-3.5 w-3.5" />
                      部门权限
                    </TabsTrigger>
                    <TabsTrigger value="settings" className="text-xs gap-1.5">
                      <Building className="h-3.5 w-3.5" />
                      机构设置
                    </TabsTrigger>
                  </TabsList>

                  <div className="mt-4 flex-1">
                    <TabsContent value="members" className="m-0">
                      <OrganizationMembersTable
                        members={members}
                        onRemoveMember={handleRemoveMember}
                        onAddMember={() => setAddMemberModalOpen(true)}
                      />
                    </TabsContent>

                    <TabsContent value="permissions" className="m-0">
                      {permissions && (
                        <OrganizationPermissionsTab
                          permissions={permissions}
                          onSave={async (updated) => {
                            setPermissions(updated)
                            if (selectedDetail?.id) {
                              await saveOrganizationPermissions(selectedDetail.id, {
                                id: selectedDetail.id,
                                dataScope: updated.dataScope,
                                abacPoliciesJson: updated.abacPoliciesJson,
                              })
                              // 刷新左侧树状列表，以便立即查看到脱敏和隐藏效果
                              await loadTree()
                              
                              // 刷新当前选中的机构详情，让右侧的头部信息也能实时反应脱敏结果
                              try {
                                const res = await organizationById(selectedDetail.id)
                                if (res.data) {
                                  setSelectedDetail(res.data)
                                }
                              } catch {
                                // ignore
                              }
                            }
                          }}
                        />
                      )}
                    </TabsContent>

                    <TabsContent value="settings" className="m-0">
                      <OrganizationSettingsTab
                        organization={selectedDetail}
                        onSave={async (updated) => {
                          await handleSaveModal({ ...selectedDetail, ...updated })
                        }}
                        onDelete={handleDelete}
                      />
                    </TabsContent>
                  </div>
                </Tabs>
              </div>
            ) : (
              <div className="flex-1 flex flex-col items-center justify-center text-muted-foreground">
                <Building className="h-12 w-12 stroke-1 mb-2 text-muted-foreground/50" />
                <p className="text-sm">请在左侧选择一个组织机构以查看详情</p>
              </div>
            )}
          </div>
        </div>

        <OrganizationModal
          open={modalOpen}
          onOpenChange={setModalOpen}
          initialData={modalInitialData}
          parentNode={modalParentNode}
          allTreeData={treeData}
          onSave={handleSaveModal}
        />

        <AddMemberModal
          open={addMemberModalOpen}
          onOpenChange={setAddMemberModalOpen}
          existingMemberIds={members.map((m) => m.userId).filter((id): id is string => Boolean(id))}
          onConfirmAdd={handleConfirmAddMembers}
        />

        <ConfirmDialog
          open={deleteConfirmOpen}
          onOpenChange={setDeleteConfirmOpen}
          title="删除组织机构"
          desc="确定要删除该组织机构吗？关联的下级节点与成员需先清空。"
          confirmText="删除"
          destructive
          handleConfirm={handleConfirmDelete}
        />
      </Main>
    </>
  )
}
