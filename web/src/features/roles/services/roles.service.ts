"use client";

import { z } from "zod";
import { fetchBff } from "@/lib/api/browser-client";
import {
  roleDetailsSchema,
  roleSchema,
  type RoleFormValues,
} from "@/features/roles/schemas/roles.schema";

export class RolesRequestError extends Error {
  constructor(
    message: string,
    readonly status: number,
    readonly code: string,
    readonly fieldErrors: Record<string, string[]> = {},
  ) {
    super(message);
  }
}

function fieldErrors(data: Record<string, unknown>): Record<string, string[]> {
  if (typeof data.errors !== "object" || data.errors === null || Array.isArray(data.errors)) return {};
  return Object.fromEntries(
    Object.entries(data.errors).flatMap(([field, messages]) => {
      if (!Array.isArray(messages)) return [];
      const validMessages = messages.filter((message): message is string => typeof message === "string");
      return validMessages.length ? [[field, validMessages]] : [];
    }),
  );
}

async function request<T>(path: string, schema: z.ZodType<T>, init?: RequestInit): Promise<T> {
  const response = await fetchBff(path, init);
  const body: unknown = response.status === 204 ? null : await response.json().catch(() => null);
  if (!response.ok) {
    const data = typeof body === "object" && body !== null ? body as Record<string, unknown> : {};
    throw new RolesRequestError(
      typeof data.code === "string" ? data.code : "request_failed",
      response.status,
      typeof data.code === "string" ? data.code : "request_failed",
      fieldErrors(data),
    );
  }
  return schema.parse(body);
}

async function requestNoContent(path: string, init: RequestInit): Promise<void> {
  const response = await fetchBff(path, init);
  if (response.ok) return;
  const body: unknown = await response.json().catch(() => null);
  const data = typeof body === "object" && body !== null ? body as Record<string, unknown> : {};
  throw new RolesRequestError(
    typeof data.code === "string" ? data.code : "request_failed",
    response.status,
    typeof data.code === "string" ? data.code : "request_failed",
    fieldErrors(data),
  );
}

export const rolesService = {
  list() {
    return request("/api/roles", z.array(roleSchema));
  },
  create(values: RoleFormValues) {
    return request("/api/roles", roleDetailsSchema, {
      method: "POST",
      body: JSON.stringify({
        name: values.name,
        description: values.description || null,
        copyPermissionsFromRoleId: values.copyPermissionsFromRoleId ?? null,
      }),
    });
  },
  update(id: string, values: RoleFormValues) {
    return request(`/api/roles/${id}`, roleDetailsSchema, {
      method: "PUT",
      body: JSON.stringify({ name: values.name, description: values.description || null }),
    });
  },
  remove(id: string, reassignToRoleId?: string) {
    const params = new URLSearchParams();
    if (reassignToRoleId) params.set("reassignToRoleId", reassignToRoleId);
    const query = params.size ? `?${params}` : "";
    return requestNoContent(`/api/roles/${id}${query}`, { method: "DELETE" });
  },
};
