"use client";

import type { PreferencesFormValues } from "@/features/profile/schemas/preferences.schema";
import { fetchBff } from "@/lib/api/browser-client";

export async function updatePreferences(values: PreferencesFormValues): Promise<void> {
  const response = await fetchBff("/api/auth/me/preferences", {
    method: "PUT",
    body: JSON.stringify(values),
  });
  if (!response.ok) {
    const error: unknown = await response.json().catch(() => null);
    const code = typeof error === "object" && error !== null && "code" in error && typeof error.code === "string"
      ? error.code
      : "request_failed";
    throw new Error(code);
  }
}
