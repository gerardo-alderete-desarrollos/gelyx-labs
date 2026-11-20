import { z } from "zod";

export const auditLogsPageSchema = z.object({
  items: z.array(z.object({
    id: z.string().uuid(),
    actorUserId: z.string().uuid().nullable(),
    action: z.string(),
    entityType: z.string(),
    entityId: z.string().nullable(),
    metadata: z.unknown(),
    ip: z.string().nullable(),
    createdAt: z.iso.datetime({ offset: true }),
  })),
  page: z.coerce.number().int(),
  pageSize: z.coerce.number().int(),
  totalCount: z.coerce.number().int(),
  totalPages: z.coerce.number().int(),
});
