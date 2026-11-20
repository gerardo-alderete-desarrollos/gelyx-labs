import { z } from "zod";

export const roleSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  description: z.string().nullable(),
  isSystem: z.boolean(),
  userCount: z.coerce.number().int().nonnegative(),
  permissionCount: z.coerce.number().int().nonnegative(),
});

export const roleDetailsSchema = roleSchema.extend({
  createdAt: z.iso.datetime({ offset: true }),
  permissions: z.array(z.string()),
});

export const roleFormSchema = z.object({
  name: z.string().trim().min(1, "requiredName").max(50, "roleNameTooLong"),
  description: z.string().trim().max(500, "roleDescriptionTooLong"),
  copyPermissionsFromRoleId: z.union([z.string().uuid(), z.literal("")]).optional().transform((value) => value || undefined),
});

export type Role = z.infer<typeof roleSchema>;
export type RoleDetails = z.infer<typeof roleDetailsSchema>;
export type RoleFormInput = z.input<typeof roleFormSchema>;
export type RoleFormValues = z.infer<typeof roleFormSchema>;
