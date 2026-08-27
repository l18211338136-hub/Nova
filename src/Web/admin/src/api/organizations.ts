import axios from 'axios'

export interface OrganizationTreeDto {
  id: string
  parentId: string | null
  name: string
  code: string | null
  type: string | null
  level: number
  sort: number
  isEnabled: boolean
  memberCount: number
  children: OrganizationTreeDto[]
}

export interface OrganizationDto {
  id: string
  parentId: string | null
  name: string
  code: string | null
  type: string | null
  level: number
  sort: number
  leaderUserId: string | null
  leaderUserName?: string | null
  phone: string | null
  email: string | null
  isEnabled: boolean
  remarks: string | null
  createdAt?: string
  memberCount?: number
}

export interface OrganizationMemberDto {
  userId: string
  organizationId: string
  userName: string
  nickName?: string | null
  email?: string | null
  isPrimary: boolean
  jobTitle?: string | null
  isLeader: boolean
  joinedAt: string
}

export interface OrganizationPermissionsDto {
  organizationId: string
  roleIds: string[]
  dataScope: number
}

export const getOrganizationTree = async (): Promise<OrganizationTreeDto[]> => {
  const res = await axios.get('/api/organizations/tree')
  return res.data?.data || []
}

export const getOrganizationById = async (id: string): Promise<OrganizationDto> => {
  const res = await axios.get(`/api/organizations/${id}`)
  return res.data?.data
}

export const createOrganization = async (data: Partial<OrganizationDto>): Promise<OrganizationDto> => {
  const res = await axios.post('/api/organizations', data)
  return res.data?.data
}

export const updateOrganization = async (id: string, data: Partial<OrganizationDto>): Promise<OrganizationDto> => {
  const res = await axios.put(`/api/organizations/${id}`, data)
  return res.data?.data
}

export const deleteOrganization = async (id: string): Promise<boolean> => {
  const res = await axios.delete(`/api/organizations/${id}`)
  return res.data?.data
}

export const getOrganizationMembers = async (id: string): Promise<OrganizationMemberDto[]> => {
  const res = await axios.get(`/api/organizations/${id}/members`)
  return res.data?.data || []
}

export const updateOrganizationMembers = async (id: string, members: Partial<OrganizationMemberDto>[]): Promise<boolean> => {
  const res = await axios.post(`/api/organizations/${id}/members`, members)
  return res.data?.data
}

export const getOrganizationPermissions = async (id: string): Promise<OrganizationPermissionsDto> => {
  const res = await axios.get(`/api/organizations/${id}/permissions`)
  return res.data?.data
}

export const saveOrganizationPermissions = async (id: string, data: OrganizationPermissionsDto): Promise<boolean> => {
  const res = await axios.put(`/api/organizations/${id}/permissions`, data)
  return res.data?.data
}
