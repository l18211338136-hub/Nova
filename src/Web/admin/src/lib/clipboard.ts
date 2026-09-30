/**
 * 跨上下文可靠的文本复制。
 *
 * - 优先使用 navigator.clipboard.writeText（仅在安全上下文 HTTPS / localhost 可用）。
 * - 不可用时回退到临时 textarea + document.execCommand('copy')，
 *   兼容 HTTP 部署（如远程服务器非 HTTPS 访问）以及被权限策略拦截的场景。
 *
 * 复制失败时抛出错误，调用方自行决定提示文案。
 */
export async function copyToClipboard(text: string): Promise<void> {
  if (typeof navigator !== 'undefined' && navigator.clipboard?.writeText) {
    try {
      await navigator.clipboard.writeText(text)
      return
    } catch {
      // 落到下方兜底（例如权限被拒 / 被拦截）
    }
  }

  if (typeof document === 'undefined') {
    throw new Error('Clipboard unavailable in current environment')
  }

  const textarea = document.createElement('textarea')
  textarea.value = text
  textarea.setAttribute('readonly', '')
  textarea.style.position = 'fixed'
  textarea.style.top = '-9999px'
  textarea.style.left = '-9999px'
  textarea.style.opacity = '0'
  document.body.appendChild(textarea)
  textarea.select()
  textarea.setSelectionRange(0, text.length)

  let ok = false
  try {
    ok = document.execCommand('copy')
  } catch {
    ok = false
  } finally {
    document.body.removeChild(textarea)
  }

  if (!ok) throw new Error('Copy command failed')
}
