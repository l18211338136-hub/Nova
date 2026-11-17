import { describe, expect, it } from 'vitest'
import type { McpToolDto } from '../types'
import {
  buildToolFilter,
  mergeToolPages,
  shouldAutoExpand,
  summarizeLoaded,
} from './tool-paging'

const tool = (id: string): McpToolDto => ({
  id,
  serverId: 'srv',
  name: id,
  description: '',
  isEnabled: true,
  isPublic: false,
  routePath: `/api/${id}`,
  httpMethod: 'GET',
  createdAt: '2026-11-17T00:00:00Z',
})

describe('buildToolFilter', () => {
  it('无搜索时只按服务过滤', () => {
    expect(buildToolFilter('srv-1')).toBe('ServerId eq srv-1')
  })

  it('有搜索时组合 contains（Name / RoutePath）', () => {
    expect(buildToolFilter('srv-1', 'abc')).toBe(
      "ServerId eq srv-1 and (contains(Name,'abc') or contains(RoutePath,'abc'))"
    )
  })

  it('搜索词去首尾空格后仍拼在同一表达式里', () => {
    expect(buildToolFilter('srv-1', '  abc  ')).toContain("contains(Name,'abc')")
  })

  it('单引号转义为两个单引号', () => {
    expect(buildToolFilter('srv-1', "o'brien")).toContain("contains(Name,'o''brien')")
  })

  it('纯空格视为无搜索', () => {
    expect(buildToolFilter('srv-1', '   ')).toBe('ServerId eq srv-1')
  })
})

describe('mergeToolPages', () => {
  it('按顺序拼接多页', () => {
    expect(mergeToolPages([[tool('a')], [tool('b')]]).map((t) => t.id)).toEqual(['a', 'b'])
  })

  it('跨页重复 id 只保留首次出现', () => {
    const merged = mergeToolPages([
      [tool('a'), tool('b')],
      [tool('b'), tool('c')],
    ])
    expect(merged.map((t) => t.id)).toEqual(['a', 'b', 'c'])
  })

  it('空页不影响结果', () => {
    expect(mergeToolPages([[tool('a')], []]).map((t) => t.id)).toEqual(['a'])
  })

  it('全空返回空数组', () => {
    expect(mergeToolPages([])).toEqual([])
    expect(mergeToolPages([[]])).toEqual([])
  })
})

describe('summarizeLoaded', () => {
  it('未加载完：remaining 与 hasMore', () => {
    expect(summarizeLoaded(1000, 1200)).toEqual({
      loaded: 1000,
      total: 1200,
      remaining: 200,
      hasMore: true,
    })
  })

  it('已加载完：hasMore=false', () => {
    expect(summarizeLoaded(1200, 1200)).toEqual({
      loaded: 1200,
      total: 1200,
      remaining: 0,
      hasMore: false,
    })
  })

  it('空集合不除零、不出现负数', () => {
    expect(summarizeLoaded(0, 0)).toEqual({ loaded: 0, total: 0, remaining: 0, hasMore: false })
  })

  it('已加载超过总数（数据缩水）时 remaining 不转负', () => {
    expect(summarizeLoaded(150, 100)).toEqual({
      loaded: 150,
      total: 100,
      remaining: 0,
      hasMore: false,
    })
  })
})

describe('shouldAutoExpand', () => {
  it('等于阈值时展开', () => {
    expect(shouldAutoExpand(200, 200)).toBe(true)
  })

  it('超过阈值折叠', () => {
    expect(shouldAutoExpand(201, 200)).toBe(false)
  })

  it('小服务正常展开', () => {
    expect(shouldAutoExpand(22, 200)).toBe(true)
  })
})
