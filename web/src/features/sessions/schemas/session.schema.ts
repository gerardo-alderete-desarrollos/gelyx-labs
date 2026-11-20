import { z } from "zod";

export const activeSessionSchema = z.object({
  id: z.string().uuid(),
  createdAt: z.string().datetime({ offset: true }),
  expiresAt: z.string().datetime({ offset: true }),
  createdByIp: z.string().nullable(),
  agent: z.string().nullable(),
  current: z.boolean(),
});

export const activeSessionsSchema = z.array(activeSessionSchema);

export const revokeOtherSessionsSchema = z.object({
  revokedCount: z.number().int().nonnegative(),
});
