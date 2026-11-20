import { z } from "zod";

export const appSettingSchema = z.object({
  key: z.string().min(1),
  value: z.union([z.string(), z.boolean()]),
  type: z.enum(["string", "boolean"]),
  updatedAt: z.iso.datetime({ offset: true }),
  updatedBy: z.string().uuid().nullable(),
});

export const settingsSchema = z.array(appSettingSchema);
