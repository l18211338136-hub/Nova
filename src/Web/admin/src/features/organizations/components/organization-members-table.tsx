import { useState } from 'react'
import { Crown, Mail, Plus, Trash2, User, UserCheck } from 'lucide-react'
import { OrganizationMemberDto } from '@/api/model'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { DictTag } from '@/components/dict/dict-tag'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'

interface OrganizationMembersTableProps {
  members: OrganizationMemberDto[]
  onRemoveMember: (userId: string) => void
  onAddMember: () => void
}

export function OrganizationMembersTable({
  members,
  onRemoveMember,
  onAddMember,
}: OrganizationMembersTableProps) {
  const [search, setSearch] = useState('')

  const filteredMembers = members.filter(
    (m) =>
      (m.userName || '').toLowerCase().includes(search.toLowerCase()) ||
      (m.jobTitle || '').toLowerCase().includes(search.toLowerCase())
  )

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <Input
          placeholder="搜索成员姓名、职位..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="max-w-xs h-8 text-xs"
        />
        <Button size="sm" className="h-8 text-xs gap-1" onClick={onAddMember}>
          <Plus className="h-3.5 w-3.5" />
          添加成员
        </Button>
      </div>

      <div className="border rounded-md">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead className="w-[200px]">成员名称</TableHead>
              <TableHead>主机构</TableHead>
              <TableHead>职务 / 岗位</TableHead>
              <TableHead>角色标识</TableHead>
              <TableHead>加入时间</TableHead>
              <TableHead className="text-right">操作</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {filteredMembers.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6} className="h-24 text-center text-xs text-muted-foreground">
                  当前部门暂无相关成员
                </TableCell>
              </TableRow>
            ) : (
              filteredMembers.map((member, idx) => (
                <TableRow key={member.userId || idx}>
                  <TableCell className="font-medium">
                    <div className="flex items-center gap-2">
                      <div className="h-7 w-7 rounded-full bg-primary/10 flex items-center justify-center text-primary text-xs font-semibold">
                        <User className="h-3.5 w-3.5" />
                      </div>
                      <div>
                        <div className="text-xs font-semibold">{member.nickName || member.userName || '未知成员'}</div>
                        <div className="text-[11px] text-muted-foreground flex items-center gap-1">
                          <Mail className="h-3 w-3" />
                          {member.email || (member.userName ? `${member.userName}@nova.com` : 'dept@nova.com')}
                        </div>
                      </div>
                    </div>
                  </TableCell>
                  <TableCell>
                    {member.isPrimary ? (
                      <Badge variant="default" className="text-[10px] gap-1">
                        <UserCheck className="h-3 w-3" />
                        主部门
                      </Badge>
                    ) : (
                      <Badge variant="outline" className="text-[10px]">
                        兼任部门
                      </Badge>
                    )}
                  </TableCell>
                  <TableCell>
                    {member.jobTitle ? (
                      <DictTag
                        code="sys_job_title"
                        value={member.jobTitle}
                        fallbackLabel={member.jobTitle}
                        className="text-xs font-normal"
                      />
                    ) : (
                      <span className="text-xs text-muted-foreground">未设置职位</span>
                    )}
                  </TableCell>
                  <TableCell>
                    {member.isLeader ? (
                      <Badge variant="secondary" className="text-[10px] gap-1 text-amber-600 bg-amber-50">
                        <Crown className="h-3 w-3" />
                        部门主管
                      </Badge>
                    ) : (
                      <span className="text-xs text-muted-foreground">普通成员</span>
                    )}
                  </TableCell>
                  <TableCell className="text-xs text-muted-foreground">
                    {member.joinedAt ? new Date(member.joinedAt).toLocaleDateString() : '-'}
                  </TableCell>
                  <TableCell className="text-right">
                    <Button
                      size="icon"
                      variant="ghost"
                      className="h-7 w-7 text-destructive hover:bg-destructive/10"
                      title="移出部门"
                      onClick={() => member.userId && onRemoveMember(member.userId)}
                    >
                      <Trash2 className="h-3.5 w-3.5" />
                    </Button>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>
    </div>
  )
}
