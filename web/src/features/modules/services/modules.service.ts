"use client";

import { z } from "zod";
import { fetchBff } from "@/lib/api/browser-client";
import {
  managedModuleSchema,
  moduleDetailsSchema,
  type ManagedModule,
  type ModuleFormValues,
} from "@/features/modules/schemas/modules.schema";

export class ModulesRequestError extends Error {
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
  return Object.fromEntries(Object.entries(data.errors).flatMap(([field, messages]) => {
    if (!Array.isArray(messages)) return [];
    const validMessages = messages.filter((message): message is string => typeof message === "string");
    return validMessages.length ? [[field, validMessages]] : [];
  }));
}

async function request<T>(path: string, schema: z.ZodType<T>, init?: RequestInit): Promise<T> {
  const response = await fetchBff(path, init);
  const body: unknown = response.status === 204 ? null : await response.json().catch(() => null);
  if (!response.ok) {
    const data = typeof body === "object" && body !== null ? body as Record<string, unknown> : {};
    const code = typeof data.code === "string" ? data.code : "request_failed";
    throw new ModulesRequestError(code, response.status, code, fieldErrors(data));
  }
  return schema.parse(body);
}

function mapModule(module: z.infer<typeof moduleDetailsSchema>): ManagedModule {
  return managedModuleSchema.parse({
    ...module,
    translations: Object.entries(module.translations).map(([locale, translation]) => ({
      locale,
      ...translation,
    })),
    roles: module.rolesWithAccess,
  });
}

export const modulesService = {
  async list() {
    const modules = await request("/api/modules", z.array(moduleDetailsSchema));
    return modules.map(mapModule);
  },
  async update(id: string, values: ModuleFormValues) {
    const moduleDetails = await request(`/api/modules/${encodeURIComponent(id)}`, moduleDetailsSchema, {
      method: "PUT",
      body: JSON.stringify({
        ...values,
        translations: Object.fromEntries(
          values.translations.map(({ locale, name, description }) => [locale, { name, description }]),
        ),
      }),
    });
    return mapModule(moduleDetails);
  },
  async updateState(id: string, isActive: boolean) {
    const response = await fetchBff(`/api/modules/${encodeURIComponent(id)}/status`, {
      method: "PATCH",
      body: JSON.stringify({ isActive }),
    });
    if (!response.ok) {
      const body: unknown = await response.json().catch(() => null);
      const data = typeof body === "object" && body !== null ? body as Record<string, unknown> : {};
      const code = typeof data.code === "string" ? data.code : "request_failed";
      throw new ModulesRequestError(code, response.status, code, fieldErrors(data));
    }
  },
};
