import { Upload } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { usePermissions } from '@/hooks/use-permissions'
import { useMcpContext } from './mcp-provider'

export function McpPrimaryButtons() {
  const { hasPermission } = usePermissions()
  const { setOpen } = useMcpContext()
  const { t } = useTranslation()

  if (!hasPermission('Mcp.Servers.Import')) return null

  return (
    <div className='flex gap-2'>
      <Button className='space-x-1' onClick={() => setOpen('import')}>
        <span>{t('Import MCP Server')}</span>
        <Upload size={18} />
      </Button>
    </div>
  )
}
