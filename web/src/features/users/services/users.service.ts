"use client";

import { z } from "zod";
import { fetchBff } from "@/lib/api/browser-client";
import {
  createUserResponseSchema,
  roleSchema,
  userDetailsSchema,
  usersPageSchema,
  type UserEditValues,
  type UserFormValues,
} from "@/features/users/schemas/users.schema";

export class ApiRequestError extends Error {
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
    throw new ApiRequestError(
      typeof data.code === "string" ? data.code : "request_failed",
      response.status,
      typeof data.code === "string" ? data.code : "request_failed",
      fieldErrors(data),
    );
  }
  if (response.status === 204) return undefined as T;
  return schema.parse(body);
}

async function requestNoContent(path: string, init: RequestInit): Promise<void> {
  const response = await fetchBff(path, init);
  if (!response.ok) {
    const body: unknown = await response.json().catch(() => null);
    const data = typeof body === "object" && body !== null ? body as Record<string, unknown> : {};
    throw new ApiRequestError(
      typeof data.code === "string" ? data.code : "request_failed",
      response.status,
      typeof data.code === "string" ? data.code : "request_failed",
      fieldErrors(data),
    );
  }
}

export const usersService = {
  list(query: URLSearchParams) {
    return request(`/api/users?${query}`, usersPageSchema);
  },
  roles() {
    return request("/api/roles", z.array(roleSchema));
  },
  details(id: string) {
    return request(`/api/users/${id}`, userDetailsSchema);
  },
  create(values: UserFormValues) {
    return request("/api/users", createUserResponseSchema, {
      method: "POST",
      body: JSON.stringify({ ...values, roleIds: values.roleIds }),
    });
  },
  async update(id: string, values: UserEditValues, rowVersion: number) {
    await requestNoContent(`/api/users/${id}`, {
      method: "PUT",
      body: JSON.stringify({ ...values, rowVersion }),
    });
  },
  async setStatus(id: string, isActive: boolean) {
    await requestNoContent(`/api/users/${id}/status`, {
      method: "PATCH",
      body: JSON.stringify({ isActive }),
    });
  },
  async assignRoles(id: string, roleIds: string[]) {
    await requestNoContent(`/api/users/${id}/roles`, {
      method: "PUT",
      body: JSON.stringify({ roleIds }),
    });
  },
  resetPassword(id: string) {
    return request(`/api/users/${id}/reset-password`, z.object({ temporaryPassword: z.string() },), {
      method: "POST",
    });
  },
  async delete(id: string) {
    await requestNoContent(`/api/users/${id}`, { method: "DELETE" });
  },
  async restore(id: string) {
    await requestNoContent(`/api/users/${id}/restore`, { method: "POST" });
  },
};
