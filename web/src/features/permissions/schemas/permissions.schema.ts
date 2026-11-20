import { z } from "zod";

const permissionSchema = z.object({ key: z.string(), name: z.string() });

export const permissionModuleSchema = z.object({
  key: z.string(),
  name: z.string(),
  description: z.string().nullable(),
  icon: z.string().nullable(),
  route: z.string().nullable(),
  sortOrder: z.coerce.number(),
  isSystem: z.boolean(),
  permissions: z.array(permissionSchema),
});

const permissionCellSchema = z.object({ key: z.string(), name: z.string(), granted: z.boolean() });
const roleModuleSchema = z.object({
  key: z.string(),
  permissions: z.array(permissionCellSchema),
});

export const permissionsMatrixSchema = z.object({
  roles: z.array(z.object({
    id: z.string().uuid(),
    name: z.string(),
    isSystem: z.boolean(),
    modules: z.array(roleModuleSchema),
  })),
  modules: z.array(z.object({
    key: z.string(),
    name: z.string(),
    isSystem: z.boolean(),
    permissions: z.array(permissionSchema),
  })),
});

export type PermissionModule = z.infer<typeof permissionModuleSchema>;
export type PermissionsMatrixData = z.infer<typeof permissionsMatrixSchema>;
