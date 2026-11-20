import { z } from "zod";

export const roleSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  description: z.string().nullable().optional(),
  isSystem: z.boolean().optional(),
});

export const userListItemSchema = z.object({
  id: z.string().uuid(),
  email: z.email(),
  fullName: z.string().nullable(),
  phoneNumber: z.string().nullable(),
  roles: z.array(z.string()),
  isActive: z.boolean(),
  isDeleted: z.boolean(),
  lastLoginAt: z.iso.datetime({ offset: true }).nullable(),
  rowVersion: z.coerce.number().int().nonnegative(),
});

export const usersPageSchema = z.object({
  items: z.array(userListItemSchema),
  page: z.coerce.number().int(),
  pageSize: z.coerce.number().int(),
  totalCount: z.coerce.number().int(),
  totalPages: z.coerce.number().int(),
});

export const userDetailsSchema = z.object({
  id: z.string().uuid(),
  email: z.email(),
  fullName: z.string().nullable(),
  phoneNumber: z.string().nullable(),
  roles: z.array(z.object({ id: z.string().uuid(), name: z.string() })),
  isActive: z.boolean(),
  isDeleted: z.boolean(),
  mustChangePassword: z.boolean(),
  lastLoginAt: z.iso.datetime({ offset: true }).nullable(),
  rowVersion: z.coerce.number().int().nonnegative(),
});

export const createUserResponseSchema = z.object({
  id: z.string().uuid(),
  email: z.email(),
  fullName: z.string(),
  temporaryPassword: z.string(),
});

export const userFormSchema = z.object({
  email: z.email("invalidEmail"),
  fullName: z.string().trim().min(1, "requiredName").max(200),
  roleIds: z.array(z.string().uuid()).min(1, "requiredRole"),
});

export const userEditSchema = z.object({
  email: z.email("invalidEmail"),
  fullName: z.string().trim().min(1, "requiredName").max(200),
  phoneNumber: z.string().max(50).nullable(),
});

export type RoleOption = z.infer<typeof roleSchema>;
export type UserListItem = z.infer<typeof userListItemSchema>;
export type UserDetails = z.infer<typeof userDetailsSchema>;
export type UsersPageData = z.infer<typeof usersPageSchema>;
export type UserFormValues = z.infer<typeof userFormSchema>;
export type UserEditValues = z.infer<typeof userEditSchema>;
