import { z } from 'zod'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Checkbox } from '@/components/ui/checkbox'
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
import { type UserDto as User } from '@/api/model'
import { useUpdateUser, useGetUserRoles, useGetUserPermissions, getGetUserRolesQueryKey, getGetUserPermissionsQueryKey } from '@/api/endpoints/users'
import { useRoles } from '@/api/endpoints/roles'
import { useGetAllPermissions, useGetPermissionGroups } from '@/api/endpoints/permissions'
import { useQueryClient } from '@tanstack/react-query'
import React, { useEffect } from 'react'

const ACTION_MAP: Record<string, string> = {
  'Read': '查询',
  'Create': '新增',
  'Update': '编辑',
  'Delete': '删除',
}

const formSchema = z.object({
  roles: z.array(z.string()).optional(),
  permissions: z.array(z.string()).optional(),
})

type PermissionsForm = z.infer<typeof formSchema>

interface Props {
  currentRow: User
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function UsersPermissionsDialog({ currentRow, open, onOpenChange }: Props) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()

  const form = useForm<PermissionsForm>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      roles: [],
      permissions: [],
    },
  })

  // Fetch roles
  const { data: allRoles } = useRoles({
    query: {
      enabled: open,
    },
    request: {
      params: {
        $top: 1000,
        $filter: "isEnabled eq true"
      }
    }
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

  // Fetch current user roles
  const { data: currentRoles } = useGetUserRoles(
    currentRow?.id ?? '',
    {
      query: {
        enabled: open && !!currentRow?.id,
      },
    }
  )

  // Fetch current user direct permissions
  const { data: currentPermissions } = useGetUserPermissions(
    currentRow?.id ?? '',
    {
      query: {
        enabled: open && !!currentRow?.id,
      },
    }
  )

  useEffect(() => {
    if (currentRoles?.data) {
      form.setValue('roles', currentRoles.data)
    }
  }, [currentRoles?.data, form])

  useEffect(() => {
    if (currentPermissions?.data) {
      form.setValue('permissions', currentPermissions.data)
    }
  }, [currentPermissions?.data, form])

  const updateMutation = useUpdateUser({
    mutation: {
      onSuccess: () => {
        toast.success(t('权限分配成功'))
        queryClient.invalidateQueries({ queryKey: ['users'] })
        if (currentRow?.id) {
          queryClient.invalidateQueries({ queryKey: getGetUserRolesQueryKey(currentRow.id) })
          queryClient.invalidateQueries({ queryKey: getGetUserPermissionsQueryKey(currentRow.id) })
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
      updateMutation.mutate({
        id: currentRow.id,
        data: {
          userName: currentRow.userName ?? '',
          email: currentRow.email ?? '',
          phoneNumber: currentRow.phoneNumber ?? '',
          isEnabled: currentRow.isEnabled ?? true,
          roles: values.roles,
          permissions: values.permissions,
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
          <DialogTitle>{t('权限分配')} - {currentRow?.userName}</DialogTitle>
          <DialogDescription>
            {t('请在下方勾选分配给该用户的角色与独立权限')}
          </DialogDescription>
        </DialogHeader>
        <div className='scroll-thin -mr-1 max-h-[60vh] overflow-y-auto pr-1'>
          <Form {...form}>
            <form id='user-permissions-form' onSubmit={form.handleSubmit(onSubmit)} className='space-y-6 p-0.5'>
              {/* Roles Section */}
              <FormField
                control={form.control}
                name="roles"
                render={({ field }) => (
                  <FormItem className='rounded-xl border bg-slate-50/50 dark:bg-slate-900/20 p-4'>
                    <div className="mb-4 pb-3 border-b border-border/50">
                      <FormLabel className="text-sm font-semibold tracking-tight">{t('角色分配')}</FormLabel>
                    </div>
                    <div className="grid grid-cols-2 gap-y-3 gap-x-4">
                      {allRoles?.data?.items?.map((role) => {
                        const isChecked = field.value?.includes(role.name ?? '');
                        return (
                          <div key={role.id} className="flex flex-row items-center space-x-3 space-y-0">
                            <Checkbox
                              checked={isChecked}
                              onCheckedChange={(checked) => {
                                return checked
                                  ? field.onChange([...(field.value || []), role.name])
                                  : field.onChange(
                                      field.value?.filter(
                                        (value) => value !== role.name
                                      )
                                    )
                              }}
                            />
                            <span 
                              className="font-normal text-sm leading-none cursor-pointer"
                              onClick={() => {
                                if (!isChecked) {
                                  field.onChange([...(field.value || []), role.name]);
                                } else {
                                  field.onChange(field.value?.filter((value) => value !== role.name));
                                }
                              }}
                            >
                              {role.displayName || role.name}
                            </span>
                          </div>
                        )
                      })}
                    </div>
                    <FormMessage />
                  </FormItem>
                )}
              />
              
              {/* Permissions Section */}
              <FormField
                control={form.control}
                name="permissions"
                render={({ field }) => (
                  <FormItem>
                    <div className="mb-4">
                      <FormLabel className="text-base">{t('独立权限分配')}</FormLabel>
                      <p className='text-sm text-muted-foreground'>
                        {t('一般情况下通过角色分配权限即可，独立权限用于特殊情况下的权限重写')}
                      </p>
                    </div>
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
                                        field.onChange(safeValue.filter((val) => val !== perm));
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
          <Button type='submit' form='user-permissions-form' disabled={isPending}>
            {t('Save changes')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
