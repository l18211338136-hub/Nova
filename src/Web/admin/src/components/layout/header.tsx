import { cn } from '@/lib/utils'
import { Separator } from '@/components/ui/separator'
import { SidebarTrigger } from '@/components/ui/sidebar'

type HeaderProps = React.HTMLAttributes<HTMLElement> & {
  fixed?: boolean
  ref?: React.Ref<HTMLElement>
}

export function Header({ className, fixed, children, ...props }: HeaderProps) {
  return (
    <header
      className={cn(
        'z-50 flex h-16 items-center gap-3 p-4 sm:gap-4',
        fixed && 'header-fixed peer/header sticky top-0 w-full bg-background/95 backdrop-blur-md border-b border-border/60 shadow-2xs',
        className
      )}
      {...props}
    >
      <SidebarTrigger variant='outline' className='max-md:scale-125' />
      <Separator orientation='vertical' className='h-6' />
      {children}
    </header>
  )
}
