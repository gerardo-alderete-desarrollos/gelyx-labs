import "server-only";

import { serverEnv } from "@/lib/env";
import { correlationHeader, isSafeCorrelationId } from "@/lib/correlation";

export function apiUrl(path: string): URL {
  return new URL(path.replace(/^\/+/, ""), `${serverEnv.API_BASE_URL.replace(/\/+$/, "")}/`);
}

export async function apiFetch(
  path: string,
  accessToken?: string,
  init: RequestInit = {},
): Promise<Response> {
  const requestHeaders = new Headers(init.headers);
  if (accessToken) {
    requestHeaders.set("Authorization", `Bearer ${accessToken}`);
  }
  if (init.body && !new Headers(init.headers).has("Content-Type")) {
    requestHeaders.set("Content-Type", "application/json");
  }

  return fetch(apiUrl(path), {
    ...init,
    headers: requestHeaders,
    cache: "no-store",
  });
}

export async function apiError(
  response: Response,
): Promise<{ code: string; message: string; correlationId?: string }> {
  const payload: unknown = await response.json().catch(() => null);
  if (typeof payload === "object" && payload !== null) {
    const body = payload as Record<string, unknown>;
    const correlationId =
      (isSafeCorrelationId(body.correlationId) && body.correlationId)
      || (isSafeCorrelationId(response.headers.get(correlationHeader)) && response.headers.get(correlationHeader))
      || undefined;
    return {
      code: typeof body.code === "string" ? body.code : "request_failed",
      message:
        typeof body.title === "string"
          ? body.title
          : typeof body.detail === "string"
            ? body.detail
            : "No se pudo completar la solicitud.",
      ...(correlationId ? { correlationId } : {}),
    };
  }

  return { code: "request_failed", message: "No se pudo completar la solicitud." };
}