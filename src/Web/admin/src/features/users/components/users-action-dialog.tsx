'use client'

import { z } from 'zod'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { zodResolver } from '@hookform/resolvers/zod'
import { useQueryClient } from '@tanstack/react-query'
import { useCreateUser, useUpdateUser } from '@/api/endpoints/users'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'
import React from 'react'
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
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import { Input } from '@/components/ui/input'
import { Switch } from '@/components/ui/switch'
import { PasswordInput } from '@/components/password-input'
import { type UserDto as User } from '@/api/model'

const getFormSchema = (t: (arg: string) => string) => z
  .object({
    userName: z.string().min(1, t('Username is required.')),
    email: z.string().email({ message: t('Please enter a valid email.') }),
    password: z.string().transform((pwd) => pwd.trim()),
    confirmPassword: z.string().transform((pwd) => pwd.trim()),
    isEnabled: z.boolean(),
    isEdit: z.boolean(),
  })
  .superRefine((data, ctx) => {
    if (!data.isEdit && !data.password) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: t('Password is required.'),
        path: ['password'],
      });
    }
    if (data.password !== data.confirmPassword) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: t("Passwords don't match."),
        path: ['confirmPassword'],
      });
    }
  })

type UserForm = z.infer<ReturnType<typeof getFormSchema>>

type UserActionDialogProps = {
  currentRow?: User
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function UsersActionDialog({
  currentRow,
  open,
  onOpenChange,
}: UserActionDialogProps) {
  const { t } = useTranslation()
  const isEdit = !!currentRow
  const queryClient = useQueryClient()
  const createMutation = useCreateUser()
  const updateMutation = useUpdateUser()
  
  const formSchema = getFormSchema(t)
  const form = useForm<UserForm>({
    resolver: zodResolver(formSchema),
    defaultValues: isEdit
      ? {
          userName: currentRow.userName,
          email: currentRow.email,
          password: '',
          confirmPassword: '',
          isEnabled: currentRow.isEnabled ?? true,
          isEdit,
        }
      : {
          userName: '',
          email: '',
          password: '',
          confirmPassword: '',
          isEnabled: true,
          isEdit,
        },
  })

  const onSubmit = (values: UserForm) => {
    if (isEdit && currentRow?.id) {
      updateMutation.mutate(
        {
          id: currentRow.id,
          data: {
            id: currentRow.id,
            userName: values.userName,
            email: values.email,
            password: values.password || undefined,
            isEnabled: values.isEnabled,
          },
        },
        {
          onSuccess: () => {
            toast.success(t('User updated successfully'))
            form.reset()
            onOpenChange(false)
            queryClient.invalidateQueries({ queryKey: ['users'] })
          },
        }
      )
    } else {
      createMutation.mutate(
        {
          data: {
            userName: values.userName,
            email: values.email,
            password: values.password,
            isEnabled: values.isEnabled,
          },
        },
        {
          onSuccess: () => {
            toast.success(t('User created successfully'))
            form.reset()
            onOpenChange(false)
            queryClient.invalidateQueries({ queryKey: ['users'] })
          },
        }
      )
    }
  }

  const isPasswordTouched = !!form.formState.dirtyFields.password

  return (
    <Dialog
      open={open}
      onOpenChange={(state) => {
        form.reset()
        onOpenChange(state)
      }}
    >
      <DialogContent className='sm:max-w-lg'>
        <DialogHeader className='text-start'>
          <DialogTitle>{isEdit ? t('Edit User') : t('Add New User')}</DialogTitle>
          <DialogDescription>
            {isEdit ? t('Update the user here.') : t('Create new user here.')}{' '}
            {t("Click save when you're done.")}
          </DialogDescription>
        </DialogHeader>
        <div className='py-1 pe-3'>
          <Form {...form}>
            <form
              id='user-form'
              onSubmit={form.handleSubmit(onSubmit)}
              className='space-y-4 px-0.5'
            >
              <FormField
                control={form.control}
                name='userName'
                render={({ field }) => (
                  <FormItem className='grid grid-cols-6 items-center space-y-0 gap-x-4 gap-y-1'>
                    <FormLabel className='col-span-2 text-end'>
                      {t('Username')}
                    </FormLabel>
                    <FormControl>
                        <Input
                          placeholder='john_doe'
                          className='col-span-4'
                          disabled={isEdit}
                          {...field}
                        />
                    </FormControl>
                    <FormMessage className='col-span-4 col-start-3' />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name='email'
                render={({ field }) => (
                  <FormItem className='grid grid-cols-6 items-center space-y-0 gap-x-4 gap-y-1'>
                    <FormLabel className='col-span-2 text-end'>{t('Email')}</FormLabel>
                    <FormControl>
                      <Input
                        placeholder='john.doe@gmail.com'
                        className='col-span-4'
                        {...field}
                      />
                    </FormControl>
                    <FormMessage className='col-span-4 col-start-3' />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name='password'
                render={({ field }) => (
                  <FormItem className='grid grid-cols-6 items-center space-y-0 gap-x-4 gap-y-1'>
                    <FormLabel className='col-span-2 text-end'>
                      {t('Password')}
                    </FormLabel>
                    <FormControl>
                      <PasswordInput
                        placeholder='e.g., S3cur3P@ssw0rd'
                        className='col-span-4'
                        {...field}
                      />
                    </FormControl>
                    <FormMessage className='col-span-4 col-start-3' />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name='confirmPassword'
                render={({ field }) => (
                  <FormItem className='grid grid-cols-6 items-center space-y-0 gap-x-4 gap-y-1'>
                    <FormLabel className='col-span-2 text-end'>
                      {t('Confirm Password')}
                    </FormLabel>
                    <FormControl>
                      <PasswordInput
                        disabled={!isPasswordTouched}
                        placeholder='e.g., S3cur3P@ssw0rd'
                        className='col-span-4'
                        {...field}
                      />
                    </FormControl>
                    <FormMessage className='col-span-4 col-start-3' />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name='isEnabled'
                render={({ field }) => (
                  <FormItem className='grid grid-cols-6 items-center space-y-0 gap-x-4 gap-y-1'>
                    <FormLabel className='col-span-2 text-end'>
                      {t('Status')}
                    </FormLabel>
                    <FormControl>
                      <div className='col-span-4 flex items-center h-10'>
                        <Switch
                          checked={field.value}
                          onCheckedChange={field.onChange}
                        />
                        <span className='ms-2 text-sm text-muted-foreground'>
                          {field.value ? t('Active') : t('Inactive')}
                        </span>
                      </div>
                    </FormControl>
                    <FormMessage className='col-span-4 col-start-3' />
                  </FormItem>
                )}
              />
            </form>
          </Form>
        </div>
        <DialogFooter>
          <Button type='submit' form='user-form'>
            {t('Save changes')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
