"use client";

import { settingsSchema, appSettingSchema } from "@/features/settings/schemas/settings.schema";
import type { AppSetting, SettingValue } from "@/features/settings/types/settings.types";
import { fetchBff } from "@/lib/api/browser-client";

export class SettingsRequestError extends Error {
  constructor(readonly code: string, readonly status: number) {
    super(code);
  }
}

async function request<T>(path: string, schema: { parse(value: unknown): T }, init?: RequestInit): Promise<T> {
  const response = await fetchBff(path, init);
  const body: unknown = await response.json().catch(() => null);
  if (!response.ok) {
    const code = typeof body === "object" && body !== null && "code" in body && typeof body.code === "string"
      ? body.code
      : "request_failed";
    throw new SettingsRequestError(code, response.status);
  }
  return schema.parse(body);
}

export const settingsService = {
  list: () => request("/api/settings", settingsSchema),
  public: () => request("/api/settings/public", settingsSchema),
  update: (key: string, value: SettingValue) => request(
    `/api/settings/${encodeURIComponent(key)}`,
    appSettingSchema,
    { method: "PUT", body: JSON.stringify({ value }) },
  ),
};

export function settingValues(settings: AppSetting[]): Record<string, SettingValue> {
  return Object.fromEntries(settings.map((setting) => [setting.key, setting.value]));
}
