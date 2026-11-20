"use client";

import { fetchBff } from "@/lib/api/browser-client";
import { permissionsMatrixSchema } from "@/features/permissions/schemas/permissions.schema";

async function errorCode(response: Response): Promise<string> {
  const body: unknown = await response.json().catch(() => null);
  return typeof body === "object" && body !== null && "code" in body && typeof body.code === "string"
    ? body.code
    : "request_failed";
}

export async function getPermissionsMatrix() {
  const response = await fetchBff("/api/permissions/matrix");
  if (!response.ok) throw new Error(await errorCode(response));
  return permissionsMatrixSchema.parse(await response.json());
}

export async function saveRolePermissions(roleId: string, permissions: string[]) {
  const response = await fetchBff(`/api/permissions/roles/${roleId}`, {
    method: "PUT",
    body: JSON.stringify({ permissions }),
  });
  if (!response.ok) {
    throw new Error(await errorCode(response));
  }
}
