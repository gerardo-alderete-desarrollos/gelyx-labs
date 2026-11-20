import { z } from "zod";
import { locales } from "@/i18n/routing";

const localeSchema = z.enum(locales);

export const moduleTranslationSchema = z.object({
  locale: localeSchema,
  name: z.string(),
  description: z.string().nullable().default(null),
});

export const managedModuleSchema = z.object({
  id: z.string().uuid(),
  key: z.string().min(1),
  icon: z.string().nullable(),
  route: z.string().nullable(),
  sortOrder: z.coerce.number().int(),
  isActive: z.boolean(),
  isSystem: z.boolean(),
  translations: z.array(moduleTranslationSchema),
  roles: z.array(z.object({ id: z.string(), name: z.string() })),
});

export const moduleDetailsSchema = z.object({
  id: z.string().uuid(),
  key: z.string().min(1),
  translations: z.record(z.string(), z.object({
    name: z.string(),
    description: z.string().nullable(),
  })),
  icon: z.string().nullable(),
  route: z.string().nullable(),
  sortOrder: z.number().int(),
  isSystem: z.boolean(),
  isActive: z.boolean(),
  rolesWithAccess: z.array(z.object({ id: z.string().uuid(), name: z.string() })),
});

export const moduleFormSchema = z.object({
  icon: z.string().min(1, "requiredIcon"),
  sortOrder: z.coerce.number().int("invalidSortOrder").min(0, "invalidSortOrder"),
  translations: z.array(z.object({
    locale: localeSchema,
    name: z.string().trim().min(1, "requiredModuleName").max(100, "moduleNameTooLong"),
    description: z.string().trim().max(500, "moduleDescriptionTooLong"),
  })).length(locales.length),
});

export type ManagedModule = z.infer<typeof managedModuleSchema>;
export type ModuleFormInput = z.input<typeof moduleFormSchema>;
export type ModuleFormValues = z.infer<typeof moduleFormSchema>;
