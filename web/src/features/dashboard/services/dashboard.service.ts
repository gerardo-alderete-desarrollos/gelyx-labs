import "server-only";

import { z } from "zod";
import { fetchServerApi } from "@/lib/api/server-client";
import type { UsersSummary } from "@/features/dashboard/types/dashboard";

const usersSummarySchema = z.object({
  total: z.coerce.number().int().nonnegative(),
  active: z.coerce.number().int().nonnegative(),
  inactive: z.coerce.number().int().nonnegative(),
  deleted: z.coerce.number().int().nonnegative(),
  recentLogins: z.array(
    z.object({
      id: z.string().uuid(),
      email: z.email(),
      fullName: z.string().nullable(),
      lastLoginAt: z.iso.datetime({ offset: true }),
    }),
  ),
});

export async function getUsersSummary(): Promise<{ summary: UsersSummary | null; error: boolean }> {
  const response = await fetchServerApi("users/summary");
  if (!response.ok) {
    return { summary: null, error: true };
  }

  return {
    summary: usersSummarySchema.parse(await response.json()),
    error: false,
  };
}
