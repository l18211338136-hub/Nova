#!/usr/bin/env node
/**
 * i18n 一致性校验
 *
 * 校验项：
 *   1. 翻译文件 JSON 合法
 *   2. 无重复 key（JSON.parse 会静默丢弃重复项，必须手工扫描原文）
 *   3. zh-CN / en-US 两侧 key 完全对称
 *   4. 无空值
 *   5. 源码中 t('...') 引用的 key 均已存在于两侧翻译文件
 *
 * 用法：pnpm check:i18n
 *
 * 注意：key 中的特殊字符可能被写成 \uXXXX 转义（例如 don\u0027t），
 * 因此所有比对都必须在 JSON.parse 之后进行，不能用字面量文本匹配。
 */
const fs = require('fs')
const path = require('path')

const ROOT = path.resolve(__dirname, '..')
const LOCALES = {
  'zh-CN': path.join(ROOT, 'src/locales/zh-CN/translation.json'),
  'en-US': path.join(ROOT, 'src/locales/en-US/translation.json'),
}
const SRC = path.join(ROOT, 'src')

let failed = false
const fail = (msg) => {
  failed = true
  console.error('  [FAIL] ' + msg)
}
const pass = (msg) => console.log('  [ ok ] ' + msg)

// ── 1 & 2：JSON 合法性 + 重复 key ──────────────────────────────
const parsed = {}
console.log('\n· 翻译文件')
for (const [tag, file] of Object.entries(LOCALES)) {
  const rel = path.relative(ROOT, file).replace(/\\/g, '/')
  let raw
  try {
    raw = fs.readFileSync(file, 'utf8')
  } catch {
    fail(rel + ' 不存在')
    continue
  }
  try {
    parsed[tag] = JSON.parse(raw)
  } catch (e) {
    fail(rel + ' JSON 非法: ' + e.message)
    continue
  }

  const seen = new Map()
  const dups = []
  const re = /^\s*("(?:\\.|[^"\\])*")\s*:/gm
  let m
  while ((m = re.exec(raw))) {
    const k = JSON.parse(m[1])
    const line = raw.slice(0, m.index).split('\n').length
    if (seen.has(k)) dups.push(JSON.stringify(k) + ' (行 ' + seen.get(k) + ' 与 ' + line + ')')
    else seen.set(k, line)
  }
  if (dups.length) {
    fail(rel + ' 存在 ' + dups.length + ' 个重复 key:')
    dups.forEach((d) => console.error('         ' + d))
  } else {
    pass(rel + '  (' + Object.keys(parsed[tag]).length + ' keys, 无重复)')
  }
}

// ── 3：两侧对称 ────────────────────────────────────────────────
console.log('\n· 语言对称性')
const [a, b] = Object.keys(LOCALES)
if (parsed[a] && parsed[b]) {
  const onlyA = Object.keys(parsed[a]).filter((k) => !(k in parsed[b]))
  const onlyB = Object.keys(parsed[b]).filter((k) => !(k in parsed[a]))
  if (onlyA.length) {
    fail('仅 ' + a + ' 有 ' + onlyA.length + ' 个 key:')
    onlyA.forEach((k) => console.error('         ' + JSON.stringify(k)))
  }
  if (onlyB.length) {
    fail('仅 ' + b + ' 有 ' + onlyB.length + ' 个 key:')
    onlyB.forEach((k) => console.error('         ' + JSON.stringify(k)))
  }
  if (!onlyA.length && !onlyB.length) pass(a + ' / ' + b + ' key 完全对称')
}

// ── 4：空值 ────────────────────────────────────────────────────
console.log('\n· 空值')
const empties = []
for (const [tag, obj] of Object.entries(parsed))
  for (const [k, v] of Object.entries(obj))
    if (typeof v !== 'string' || !v.trim()) empties.push(tag + ' → ' + JSON.stringify(k))
if (empties.length) {
  fail('存在 ' + empties.length + ' 个空值:')
  empties.forEach((e) => console.error('         ' + e))
} else pass('无空值')

// ── 5：源码 t() 引用 ───────────────────────────────────────────
console.log('\n· 源码 t() 引用')
const files = []
;(function walk(dir) {
  for (const e of fs.readdirSync(dir, { withFileTypes: true })) {
    const p = path.join(dir, e.name)
    if (e.isDirectory()) {
      if (!/node_modules|[.]git|locales/.test(p)) walk(p)
    } else if (/[.](tsx|ts)$/.test(e.name) && !/[.]d[.]ts$/.test(e.name)) {
      files.push(p)
    }
  }
})(SRC)

// 只匹配字符串字面量形式的 t('...')；动态 key t(variable) 无法静态校验，跳过
const callRe = new RegExp("\\bt\\(\\s*(['\"])((?:\\\\.|(?!\\1)[^\\\\])*)\\1\\s*[,)]", 'g')
const missing = new Map()
for (const f of files) {
  const src = fs.readFileSync(f, 'utf8')
  let m
  while ((m = callRe.exec(src))) {
    const key = m[2].replace(/\\'/g, "'").replace(/\\"/g, '"').replace(/\\n/g, '\n')
    if (!key.trim()) continue
    const absent = Object.keys(LOCALES).filter((t) => parsed[t] && !(key in parsed[t]))
    if (!absent.length) continue
    if (!missing.has(key)) missing.set(key, { langs: absent, files: new Set() })
    missing.get(key).files.add(path.relative(ROOT, f).replace(/\\/g, '/'))
  }
}
if (missing.size) {
  fail('有 ' + missing.size + ' 个 key 未翻译:')
  for (const [k, v] of [...missing].sort())
    console.error(
      '         ' + JSON.stringify(k) + '  [缺 ' + v.langs.join('/') + ']\n' +
      '             ← ' + [...v.files].join(', ')
    )
} else {
  pass('已扫描 ' + files.length + ' 个源文件，无缺失 key')
}

console.log('\n' + (failed ? 'i18n 校验未通过' : 'i18n 校验通过') + '\n')
process.exit(failed ? 1 : 0)
