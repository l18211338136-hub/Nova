import { z } from 'zod'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'

import {
  Form,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import { type RoleDto as Role } from '@/api/model'
import { useUpdateRole, useGetRolePermissions, getGetRolePermissionsQueryKey } from '@/api/endpoints/roles'
import { useGetAllPermissions, useGetPermissionGroups } from '@/api/endpoints/permissions'
import { useMenus } from '@/api/endpoints/menus'
import { useQueryClient } from '@tanstack/react-query'
import React, { useEffect } from 'react'

const ACTION_MAP: Record<string, string> = {
  'Read': '查询',
  'Create': '新增',
  'Update': '编辑',
  'Delete': '删除',
  'ChangePassword': '修改密码',
  'ResetPassword': '重置密码',
}

const formSchema = z.object({
  permissions: z.array(z.string()).optional(),
  menus: z.array(z.string()).optional(),
})

type PermissionsForm = z.infer<typeof formSchema>

interface Props {
  currentRow: Role
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function RolesPermissionsDialog({ currentRow, open, onOpenChange }: Props) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()

  const form = useForm<PermissionsForm>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      permissions: [],
      menus: [],
    },
  })

  // Fetch all system permissions
  const { data: allPermissions } = useGetAllPermissions({
    query: {
      enabled: open,
    },
  })

  // Fetch permission group names dictionary
  const { data: permissionGroups } = useGetPermissionGroups({
    query: {
      enabled: open,
    },
  })

  // Fetch all menus (without specific permission requirement)
  const { data: menusResponse } = useMenus({
    request: {
      params: {
        $top: 1000,
        $orderby: 'Sort asc, CreatedAt asc'
      }
    },
    query: {
      enabled: open,
    }
  })

  // Fetch current role permissions
  const { data: currentPermissions } = useGetRolePermissions(
    currentRow?.id ?? '',
    {
      query: {
        enabled: open && !!currentRow?.id,
      },
    }
  )

  // Sync current permissions to form when data is loaded
  useEffect(() => {
    if (currentPermissions?.data) {
      form.setValue('permissions', currentPermissions.data.permissions || [])
      form.setValue('menus', currentPermissions.data.menus || [])
    }
  }, [currentPermissions?.data, form])

  const updateMutation = useUpdateRole({
    mutation: {
      onSuccess: () => {
        toast.success(t('权限分配成功'))
        queryClient.invalidateQueries({ queryKey: ['roles'] })
        if (currentRow?.id) {
          queryClient.invalidateQueries({ queryKey: getGetRolePermissionsQueryKey(currentRow.id) })
        }
        onOpenChange(false)
      },
      onError: (error: any) => {
        toast.error(t('权限分配失败'), {
          description: error?.response?.data?.title || error.message,
        })
      }
    }
  })

  const onSubmit = (values: PermissionsForm) => {
    if (currentRow?.id) {
      // 自动推导所需菜单
      const allMenus = menusResponse?.data?.items || []
      const selectedMenuIds = new Set<string>()

      if (values.permissions && permissionGroups?.data) {
        values.permissions.forEach(perm => {
          const group = perm.split('.').slice(0, 2).join('.')
          const groupName = permissionGroups?.data?.[group]

          if (groupName) {
            const matchingMenu = allMenus.find(m => m.name === groupName)
            if (matchingMenu && matchingMenu.id) {
              selectedMenuIds.add(matchingMenu.id)
              
              // 递归添加父节点
              let parentId = matchingMenu.parentId
              while (parentId) {
                selectedMenuIds.add(parentId)
                const parentMenu = allMenus.find(m => m.id === parentId)
                parentId = parentMenu?.parentId
              }
            }
          }
        })
      }

      updateMutation.mutate({
        id: currentRow.id,
        data: {
          name: currentRow.name ?? '',
          displayName: currentRow.displayName ?? '',
          sort: currentRow.sort ?? 0,
          isEnabled: currentRow.isEnabled ?? true,
          remarks: currentRow.remarks,
          permissions: values.permissions,
          menus: Array.from(selectedMenuIds),
        },
      })
    }
  }

  const isPending = updateMutation.isPending

  const groupedPermissions = React.useMemo(() => {
    if (!allPermissions?.data) return {}
    return allPermissions.data.reduce((acc, perm) => {
      const parts = perm.split('.')
      const prefix = parts.length > 1 ? `${parts[0]}.${parts[1]}` : parts[0]
      if (!acc[prefix]) acc[prefix] = []
      acc[prefix].push(perm)
      return acc
    }, {} as Record<string, string[]>)
  }, [allPermissions?.data])



  return (
    <Dialog
      open={open}
      onOpenChange={(state) => {
        form.reset()
        onOpenChange(state)
      }}
    >
      <DialogContent className='sm:max-w-2xl'>
        <DialogHeader className='text-left'>
          <DialogTitle>{t('权限分配')} - {currentRow?.displayName || currentRow?.name}</DialogTitle>
          <DialogDescription>
            {t('请在下方勾选分配给该角色的权限')}
          </DialogDescription>
        </DialogHeader>
        <div className='scroll-thin -mr-1 max-h-[60vh] overflow-y-auto pr-1'>
          <Form {...form}>
            <form id='permissions-form' onSubmit={form.handleSubmit(onSubmit)} className='space-y-4 p-0.5'>
              <FormField
                control={form.control}
                name="permissions"
                render={({ field }) => (
                  <FormItem>
                    <div className="space-y-6">
                      {Object.entries(groupedPermissions).map(([group, perms]) => {
                        const safeValue = Array.isArray(field.value) ? field.value : []
                        const allSelected = perms.every((p) => safeValue.includes(p))
                        const someSelected = perms.some((p) => safeValue.includes(p))
                        
                        return (
                          <div key={group} className="rounded-xl border bg-slate-50/50 dark:bg-slate-900/20 p-4 transition-colors hover:bg-slate-50 dark:hover:bg-slate-900/50">
                            <div className="flex justify-between items-center mb-4 pb-3 border-b border-border/50">
                              <h4 className="text-sm font-semibold tracking-tight">
                                {permissionGroups?.data?.[group] || group}
                              </h4>
                              <div 
                                className="flex items-center space-x-2 cursor-pointer group"
                                onClick={(e) => {
                                  e.preventDefault();
                                  e.stopPropagation();
                                  if (!allSelected) {
                                    const newValues = Array.from(new Set([...safeValue, ...perms]));
                                    field.onChange(newValues);
                                  } else {
                                    const newValues = safeValue.filter(v => !perms.includes(v));
                                    field.onChange(newValues);
                                  }
                                }}
                              >
                                <div className={cn(
                                  "flex size-4 shrink-0 items-center justify-center rounded-md border shadow-xs transition-colors",
                                  allSelected || someSelected 
                                    ? "bg-primary border-primary text-primary-foreground" 
                                    : "border-input bg-transparent dark:bg-input/30"
                                )}>
                                  {allSelected && (
                                    <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round"><polyline points="20 6 9 17 4 12"></polyline></svg>
                                  )}
                                  {(!allSelected && someSelected) && (
                                    <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round"><line x1="5" y1="12" x2="19" y2="12"></line></svg>
                                  )}
                                </div>
                                <span className="text-xs font-medium text-muted-foreground group-hover:text-foreground transition-colors">
                                  {t('全选')}
                                </span>
                              </div>
                            </div>
                            <div className="flex flex-wrap gap-2.5">
                              {perms.map((perm) => {
                                const action = perm.split('.').pop() || perm;
                                const label = ACTION_MAP[action] || action;
                                const isChecked = safeValue.includes(perm);
                                
                                return (
                                  <div
                                    key={perm}
                                    onClick={(e) => {
                                      e.preventDefault();
                                      e.stopPropagation();
                                      if (!isChecked) {
                                        field.onChange([...safeValue, perm]);
                                      } else {
                                        field.onChange(safeValue.filter((value) => value !== perm));
                                      }
                                    }}
                                    className={cn(
                                      "inline-flex items-center justify-center px-3.5 py-1.5 rounded-full text-xs font-medium transition-all cursor-pointer select-none border",
                                      isChecked
                                        ? "bg-primary text-primary-foreground border-primary shadow-sm hover:bg-primary/90"
                                        : "bg-secondary/50 text-secondary-foreground border-transparent hover:bg-secondary hover:shadow-sm"
                                    )}
                                  >
                                    {label}
                                  </div>
                                )
                              })}
                            </div>
                          </div>
                        )
                      })}
                    </div>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </form>
          </Form>
        </div>
        <DialogFooter>
          <Button type='button' variant='outline' onClick={() => onOpenChange(false)}>
            {t('Cancel')}
          </Button>
          <Button type='submit' form='permissions-form' disabled={isPending}>
            {t('Save changes')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
