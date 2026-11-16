import { describe, expect, it } from 'vitest'
import type { McpToolDto } from '../types'
import {
  deriveGroupFromPath,
  deriveToolGroup,
  filterToolGroups,
  groupTools,
} from './mcp-tool-groups'

const tool = (over: Partial<McpToolDto>): McpToolDto => ({
  id: over.id ?? 'id',
  serverId: 'srv',
  name: over.name ?? 'tool',
  description: over.description ?? '',
  isEnabled: true,
  isPublic: false,
  routePath: over.routePath ?? '/x',
  httpMethod: over.httpMethod ?? 'GET',
  createdAt: '2026-10-02T00:00:00Z',
  group: over.group,
})

describe('deriveGroupFromPath（与后端 DeriveGroup 对齐）', () => {
  it('取 /api 后的第二段', () => {
    expect(deriveGroupFromPath('/api/identity/login')).toBe('/api/identity')
    expect(deriveGroupFromPath('/api/Mcp/servers/{id}')).toBe('/api/Mcp')
  })

  it('首段不是 api 时取首段', () => {
    expect(deriveGroupFromPath('/repos/{owner}/{repo}')).toBe('/repos')
    expect(deriveGroupFromPath('/v1/charges')).toBe('/v1')
  })

  it('大小写不敏感地识别 api 前缀', () => {
    expect(deriveGroupFromPath('/API/Users')).toBe('/API/Users')
  })

  it('空路径回退到 /', () => {
    expect(deriveGroupFromPath('')).toBe('/')
    expect(deriveGroupFromPath('/')).toBe('/')
  })
})

describe('deriveToolGroup', () => {
  it('优先使用后端落库的 group', () => {
    expect(deriveToolGroup(tool({ routePath: '/api/a/x', group: '自定义分组' }))).toBe('自定义分组')
  })

  it('group 为空白时回退到推导', () => {
    expect(deriveToolGroup(tool({ routePath: '/api/a/x', group: '  ' }))).toBe('/api/a')
  })
})

describe('groupTools', () => {
  it('按分组聚合并按名称升序', () => {
    const groups = groupTools([
      tool({ id: '1', routePath: '/api/b/x' }),
      tool({ id: '2', routePath: '/api/a/x' }),
      tool({ id: '3', routePath: '/api/a/y' }),
    ])
    expect(groups.map((g) => g.name)).toEqual(['/api/a', '/api/b'])
    expect(groups[0].tools).toHaveLength(2)
  })

  it('组内按 routePath 再按 method 排序', () => {
    const groups = groupTools([
      tool({ id: '1', routePath: '/api/a/z', httpMethod: 'GET' }),
      tool({ id: '2', routePath: '/api/a/a', httpMethod: 'POST' }),
      tool({ id: '3', routePath: '/api/a/a', httpMethod: 'DELETE' }),
    ])
    expect(groups[0].tools.map((x) => `${x.httpMethod} ${x.routePath}`)).toEqual([
      'DELETE /api/a/a',
      'POST /api/a/a',
      'GET /api/a/z',
    ])
  })
})

describe('filterToolGroups', () => {
  const groups = groupTools([
    tool({ id: '1', routePath: '/api/users', name: 'users_list', description: '查询用户' }),
    tool({ id: '2', routePath: '/api/audit/logs', name: 'audit_logs', description: '审计日志' }),
  ])

  it('空关键字原样返回', () => {
    expect(filterToolGroups(groups, '   ')).toHaveLength(2)
  })

  it('按路径 / 名称 / 描述 / 方法命中', () => {
    expect(filterToolGroups(groups, 'audit')).toHaveLength(1)
    expect(filterToolGroups(groups, '查询')).toHaveLength(1)
    expect(filterToolGroups(groups, 'users')).toHaveLength(1)
  })

  it('分组名命中时保留整组', () => {
    const result = filterToolGroups(groups, '/api/audit')
    expect(result).toHaveLength(1)
    expect(result[0].tools).toHaveLength(1)
  })

  it('全部不命中时返回空数组', () => {
    expect(filterToolGroups(groups, 'zzz')).toEqual([])
  })
})
