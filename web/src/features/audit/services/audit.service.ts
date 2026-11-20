"use client";

import { fetchBff } from "@/lib/api/browser-client";
import { auditLogsPageSchema } from "@/features/audit/schemas/audit.schema";

export async function getAuditLogs(params: URLSearchParams) {
  const response = await fetchBff(`/api/audit-logs?${params}`);
  if (!response.ok) {
    const body: unknown = await response.json().catch(() => null);
    const code = typeof body === "object" && body !== null && "code" in body && typeof body.code === "string"
      ? body.code
      : "request_failed";
    throw new Error(code);
  }
  return auditLogsPageSchema.parse(await response.json());
}
