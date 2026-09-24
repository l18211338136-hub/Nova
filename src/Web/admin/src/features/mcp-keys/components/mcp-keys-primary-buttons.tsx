import { KeyRound } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { usePermissions } from '@/hooks/use-permissions'
import { useMcpKeysContext } from './mcp-keys-provider'

export function McpKeysPrimaryButtons() {
  const { hasPermission } = usePermissions()
  const { setOpen } = useMcpKeysContext()
  const { t } = useTranslation()
  return (
    <div className='flex gap-2'>
      {hasPermission('Mcp.Keys.Create') && (
        <Button className='space-x-1' onClick={() => setOpen('create')}>
          <span>{t('New MCP Key')}</span> <KeyRound size={18} />
        </Button>
      )}
    </div>
  )
}
