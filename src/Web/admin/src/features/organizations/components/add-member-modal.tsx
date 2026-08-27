import { useState } from 'react'
import { Briefcase, CheckCircle2, ChevronLeft, ChevronRight, Search, User, UserPlus } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { Checkbox } from '@/components/ui/checkbox'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { useUsers } from '@/api/endpoints/users'
import { DictSelect } from '@/components/dict/dict-select'

export interface SelectedMemberCandidate {
  userId: string
  userName: string
  nickName?: string
  email?: string
  jobTitle?: string
  isPrimary: boolean
}

interface AddMemberModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  existingMemberIds: string[]
  onConfirmAdd: (newMembers: SelectedMemberCandidate[]) => void
}

export function AddMemberModal({
  open,
  onOpenChange,
  existingMemberIds,
  onConfirmAdd,
}: AddMemberModalProps) {
  const [searchQuery, setSearchQuery] = useState('')
  const [selectedUserIds, setSelectedUserIds] = useState<Record<string, boolean>>({})
  const [jobTitles, setJobTitles] = useState<Record<string, string>>({})
  const [isPrimaryMap, setIsPrimaryMap] = useState<Record<string, boolean>>({})

  // Pagination state
  const [page, setPage] = useState(1)
  const pageSize = 5

  // Query system users with OData pagination & search filter
  const { data: usersData, isLoading } = useUsers({
    query: {
      queryKey: ['add-member-users', page, pageSize, searchQuery],
    },
    request: {
      params: {
        $count: true,
        $skip: (page - 1) * pageSize,
        $top: pageSize,
        ...(searchQuery.trim() && {
          $filter: `contains(userName, '${searchQuery.trim()}') or contains(email, '${searchQuery.trim()}')`,
        }),
      },
    },
  })

  const rawUsers = (usersData?.data as any)?.items || usersData?.data || []
  const userList = Array.isArray(rawUsers)
    ? rawUsers.map((u: any) => ({
        id: u.id || '',
        userName: u.userName || '未知用户',
        nickName: u.userName || '未知用户',
        email: u.email || (u.userName ? `${u.userName}@nova.com` : '-'),
      }))
    : []

  const totalCount = (usersData?.data as any)?.total ?? userList.length
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))

  const filteredUsers = userList.filter(
    (u) =>
      u.userName.toLowerCase().includes(searchQuery.toLowerCase()) ||
      u.nickName.toLowerCase().includes(searchQuery.toLowerCase()) ||
      u.email.toLowerCase().includes(searchQuery.toLowerCase())
  )

  const handleToggleSelect = (userId: string) => {
    setSelectedUserIds((prev) => {
      const next = { ...prev, [userId]: !prev[userId] }
      // Set default primary true if selecting
      if (next[userId] && isPrimaryMap[userId] === undefined) {
        setIsPrimaryMap((p) => ({ ...p, [userId]: true }))
      }
      return next
    })
  }

  const handleConfirm = () => {
    const selectedList: SelectedMemberCandidate[] = []
    Object.keys(selectedUserIds).forEach((userId) => {
      if (selectedUserIds[userId]) {
        const u = userList.find((item) => item.id === userId)
        if (u) {
          selectedList.push({
            userId: u.id,
            userName: u.userName,
            nickName: u.nickName,
            email: u.email,
            jobTitle: jobTitles[userId] || '技术人员',
            isPrimary: isPrimaryMap[userId] ?? true,
          })
        }
      }
    })

    if (selectedList.length > 0) {
      onConfirmAdd(selectedList)
    }
    // Reset modal state
    setSelectedUserIds({})
    setJobTitles({})
    setIsPrimaryMap({})
    onOpenChange(false)
  }

  const selectedCount = Object.values(selectedUserIds).filter(Boolean).length

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[640px] w-[640px] h-[580px] min-h-[580px] max-h-[580px] flex flex-col justify-between p-6 overflow-hidden">
        <DialogHeader className="shrink-0 pb-1">
          <DialogTitle className="flex items-center gap-2">
            <UserPlus className="h-5 w-5 text-primary" />
            添加部门成员
          </DialogTitle>
          <DialogDescription>
            从系统用户列表中选择人员加入当前部门，可为其指定职务岗位与主机构标识。
          </DialogDescription>
        </DialogHeader>

        <div className="flex-1 flex flex-col min-h-0 space-y-3 py-1 overflow-hidden">
          {/* Search bar */}
          <div className="relative shrink-0">
            <Search className="absolute left-3 top-2.5 h-4 w-4 text-muted-foreground" />
            <Input
              placeholder="搜索用户名、姓名或邮箱..."
              value={searchQuery}
              onChange={(e) => {
                setSearchQuery(e.target.value)
                setPage(1)
              }}
              className="pl-9 h-9 text-xs"
            />
          </div>

          {/* User selection list - Fixed 310px Height */}
          <div className="h-[310px] min-h-[310px] max-h-[310px] overflow-y-auto border rounded-md divide-y divide-border bg-background flex-1">
            {isLoading ? (
              <div className="h-full flex items-center justify-center text-xs text-muted-foreground">
                正在加载系统用户...
              </div>
            ) : filteredUsers.length === 0 ? (
              <div className="h-full flex items-center justify-center text-xs text-muted-foreground">
                未找到匹配的用户
              </div>
            ) : (
              filteredUsers.map((u) => {
                const isExisting = existingMemberIds.includes(u.id)
                const isSelected = !!selectedUserIds[u.id]

                return (
                  <div
                    key={u.id}
                    className={`p-3 min-h-[56px] flex items-center justify-between transition-colors ${
                      isExisting
                        ? 'bg-muted/40 opacity-60 cursor-not-allowed'
                        : isSelected
                        ? 'bg-primary/5'
                        : 'hover:bg-accent/50'
                    }`}
                  >
                    <div className="flex items-center gap-3 min-w-0 flex-1">
                      <Checkbox
                        checked={isSelected || isExisting}
                        disabled={isExisting}
                        onCheckedChange={() => !isExisting && handleToggleSelect(u.id)}
                      />

                      <div className="h-8 w-8 rounded-full bg-primary/10 flex items-center justify-center text-primary text-xs font-semibold shrink-0">
                        <User className="h-4 w-4" />
                      </div>

                      <div className="min-w-0 flex-1">
                        <div className="flex items-center gap-2">
                          <span className="text-xs font-semibold text-foreground truncate">{u.nickName}</span>
                          <span className="text-[11px] text-muted-foreground">(@{u.userName})</span>
                          {isExisting && (
                            <Badge variant="outline" className="text-[10px] px-1 py-0 h-4 text-muted-foreground">
                              已在部门
                            </Badge>
                          )}
                        </div>
                        <div className="text-[11px] text-muted-foreground truncate">{u.email}</div>
                      </div>
                    </div>

                    {/* Job Title & Primary Checkbox when selected */}
                    {isSelected && !isExisting && (
                      <div className="flex items-center gap-2 shrink-0 ml-3">
                        <div className="flex items-center gap-1.5">
                          <Briefcase className="h-3.5 w-3.5 text-muted-foreground shrink-0" />
                          <DictSelect
                            code="sys_job_title"
                            value={jobTitles[u.id] || ''}
                            onValueChange={(val) => setJobTitles({ ...jobTitles, [u.id]: val })}
                            placeholder="选择岗位/职务"
                            className="h-8 w-36 text-xs"
                          />
                        </div>

                        <label className="flex items-center gap-1 text-[11px] text-muted-foreground cursor-pointer">
                          <Checkbox
                            checked={isPrimaryMap[u.id] ?? true}
                            onCheckedChange={(checked) =>
                              setIsPrimaryMap({ ...isPrimaryMap, [u.id]: !!checked })
                            }
                          />
                          主部门
                        </label>
                      </div>
                    )}
                  </div>
                )
              })
            )}
          </div>

          {/* Pagination Controls */}
          <div className="flex items-center justify-between text-xs pt-1 px-1 text-muted-foreground shrink-0">
            <div>
              共 <span className="font-medium text-foreground">{totalCount}</span> 条用户记录，第{' '}
              <span className="font-medium text-foreground">{page}</span> / {totalPages} 页
            </div>
            <div className="flex items-center gap-1.5">
              <Button
                variant="outline"
                size="sm"
                className="h-7 px-2 text-xs"
                disabled={page <= 1 || isLoading}
                onClick={() => setPage((p) => Math.max(1, p - 1))}
              >
                <ChevronLeft className="h-3.5 w-3.5 mr-1" />
                上一页
              </Button>
              <Button
                variant="outline"
                size="sm"
                className="h-7 px-2 text-xs"
                disabled={page >= totalPages || isLoading}
                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              >
                下一页
                <ChevronRight className="h-3.5 w-3.5 ml-1" />
              </Button>
            </div>
          </div>
        </div>

        <DialogFooter className="flex items-center justify-between border-t pt-3">
          <div className="text-xs text-muted-foreground flex items-center gap-1.5">
            <CheckCircle2 className="h-4 w-4 text-primary" />
            已选择 <span className="font-semibold text-foreground">{selectedCount}</span> 名新成员
          </div>
          <div className="flex items-center gap-2">
            <Button variant="outline" size="sm" onClick={() => onOpenChange(false)}>
              取消
            </Button>
            <Button size="sm" disabled={selectedCount === 0} onClick={handleConfirm}>
              确定添加 ({selectedCount})
            </Button>
          </div>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
