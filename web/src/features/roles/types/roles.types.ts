import type { Role, RoleFormValues } from "@/features/roles/schemas/roles.schema";

export type RoleUpdateVariables = Readonly<{ role: Role; values: RoleFormValues }>;
export type RoleDeleteVariables = Readonly<{ role: Role; targetRoleId?: string }>;
