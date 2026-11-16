import { describe, expect, it } from 'vitest'
import type { McpKeyScopeValue } from '../types'
import {
  fromRow,
  normalizeScope,
  pruneScope,
  setServerAllTools,
  setServerTools,
  summarizeScope,
  toPayload,
  toggleServer,
  toggleTool,
  validateScope,
} from './key-scope'

const A = 'server-a'
const B = 'server-b'

const selected = (scopes: McpKeyScopeValue['scopes']): McpKeyScopeValue => ({
  allServers: false,
  scopes,
})

describe('normalizeScope', () => {
  it('allServers=true 时清空 scopes', () => {
    const out = normalizeScope({ allServers: true, scopes: [{ serverId: A, toolIds: null }] })
    expect(out).toEqual({ allServers: true, scopes: [] })
  })

  it('复制数组，不共享引用', () => {
    const input = selected([{ serverId: A, toolIds: ['t1'] }])
    const out = normalizeScope(input)
    out.scopes[0].toolIds?.push('t2')
    expect(input.scopes[0].toolIds).toEqual(['t1'])
  })
})

describe('validateScope', () => {
  it('全部服务永远合法', () => {
    expect(validateScope({ allServers: true, scopes: [] })).toBeNull()
  })

  it('指定模式但没有服务 → no-server', () => {
    expect(validateScope(selected([]))).toEqual({ code: 'no-server' })
  })

  it('某服务工具为空数组 → no-tool 并带 serverId', () => {
    expect(
      validateScope(selected([{ serverId: B, toolIds: [] }, { serverId: A, toolIds: null }]))
    ).toEqual({ code: 'no-tool', serverId: B })
  })

  it('整服务开放（null）视为合法', () => {
    expect(validateScope(selected([{ serverId: A, toolIds: null }]))).toBeNull()
  })
})

describe('toggleServer', () => {
  it('新增服务默认全部工具（null）', () => {
    const out = toggleServer(selected([]), A, true)
    expect(out.scopes).toEqual([{ serverId: A, toolIds: null }])
  })

  it('取消服务不影响其它服务', () => {
    const out = toggleServer(
      selected([
        { serverId: A, toolIds: null },
        { serverId: B, toolIds: ['t1'] },
      ]),
      A,
      false
    )
    expect(out.scopes).toEqual([{ serverId: B, toolIds: ['t1'] }])
  })

  it('重复勾选同一服务不产生重复项', () => {
    const once = toggleServer(selected([]), A, true)
    const twice = toggleServer(once, A, true)
    expect(twice.scopes).toHaveLength(1)
  })
})

describe('setServerAllTools', () => {
  it('切到指定工具时使用传入的现存工具 id', () => {
    const out = setServerAllTools(selected([{ serverId: A, toolIds: null }]), A, false, ['t1', 't2'])
    expect(out.scopes[0].toolIds).toEqual(['t1', 't2'])
  })

  it('切回全部工具时为 null', () => {
    const out = setServerAllTools(selected([{ serverId: A, toolIds: ['t1'] }]), A, true)
    expect(out.scopes[0].toolIds).toBeNull()
  })
})

describe('toggleTool / setServerTools', () => {
  it('勾选与取消工具', () => {
    const start = selected([{ serverId: A, toolIds: [] }])
    const added = toggleTool(start, A, 't1')
    expect(added.scopes[0].toolIds).toEqual(['t1'])
    const removed = toggleTool(added, A, 't1')
    expect(removed.scopes[0].toolIds).toEqual([])
  })

  it('取消到 0 个仍保留空数组（交由校验拦截，不静默回退）', () => {
    const out = toggleTool(selected([{ serverId: A, toolIds: ['t1'] }]), A, 't1')
    expect(out.scopes[0].toolIds).toEqual([])
    expect(validateScope(out)).toEqual({ code: 'no-tool', serverId: A })
  })

  it('组级批量设置', () => {
    const out = setServerTools(selected([{ serverId: A, toolIds: null }]), A, ['t3'])
    expect(out.scopes[0].toolIds).toEqual(['t3'])
  })

  it('对「全部工具」的服务调用 toggleTool 不改变状态', () => {
    const out = toggleTool(selected([{ serverId: A, toolIds: null }]), A, 't1')
    expect(out.scopes[0].toolIds).toBeNull()
  })
})

describe('summarizeScope', () => {
  it('全部服务', () => {
    expect(summarizeScope({ allServers: true, scopes: [] })).toEqual({ kind: 'all' })
  })

  it('指定模式但为空', () => {
    expect(summarizeScope(selected([]))).toEqual({ kind: 'none' })
  })

  it('全部是整服务开放', () => {
    expect(
      summarizeScope(selected([{ serverId: A, toolIds: null }, { serverId: B, toolIds: null }]))
    ).toEqual({ kind: 'servers', servers: 2, tools: 0, allTools: true })
  })

  it('部分指定工具时统计工具数', () => {
    expect(
      summarizeScope(
        selected([
          { serverId: A, toolIds: null },
          { serverId: B, toolIds: ['t1', 't2', 't3'] },
        ])
      )
    ).toEqual({ kind: 'servers', servers: 2, tools: 3, allTools: false })
  })
})

describe('pruneScope', () => {
  it('移除已删除的服务，原值不变', () => {
    const input = selected([
      { serverId: A, toolIds: null },
      { serverId: B, toolIds: ['t1'] },
    ])
    const out = pruneScope(input, (id) => id === A)
    expect(out.scopes.map((s) => s.serverId)).toEqual([A])
    expect(input.scopes).toHaveLength(2)
  })

  it('可同时过滤不存在的工具', () => {
    const out = pruneScope(
      selected([{ serverId: A, toolIds: ['t1', 'gone'] }]),
      () => true,
      (_sid, tid) => tid !== 'gone'
    )
    expect(out.scopes[0].toolIds).toEqual(['t1'])
  })
})

describe('fromRow / toPayload', () => {
  it('缺字段的旧数据按「全部服务」处理', () => {
    expect(fromRow({ id: 'k1', name: 'old' })).toEqual({ allServers: true, scopes: [] })
  })

  it('还原指定范围', () => {
    expect(fromRow({ id: 'k1', allServers: false, scopes: [{ serverId: A, toolIds: null }] })).toEqual({
      allServers: false,
      scopes: [{ serverId: A, toolIds: null }],
    })
  })

  it('提交体在全部服务时 scopes 为空且名称 trimmed', () => {
    expect(toPayload('  key  ', { allServers: true, scopes: [{ serverId: A, toolIds: null }] })).toEqual({
      name: 'key',
      allServers: true,
      scopes: [],
    })
  })
})
