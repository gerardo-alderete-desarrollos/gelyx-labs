import "server-only";

import { apiFetch } from "@/lib/api/client";
import { authTokensSchema } from "@/features/auth/schemas/session.schema";
import type { AuthTokens } from "@/features/auth/types/session";

const inFlightRefreshes = new Map<string, Promise<AuthTokens>>();

export class UpstreamRefreshError extends Error {
  constructor(readonly status: number) {
    super(`Session refresh failed with status ${status}.`);
  }
}

export function refreshTokens(refreshToken: string): Promise<AuthTokens> {
  const existing = inFlightRefreshes.get(refreshToken);
  if (existing) {
    return existing;
  }

  const refresh = apiFetch("auth/refresh", undefined, {
    method: "POST",
    body: JSON.stringify({ refreshToken }),
  }).then(async (response) => {
    if (!response.ok) {
      throw new UpstreamRefreshError(response.status);
    }
    return authTokensSchema.parse(await response.json());
  });

  inFlightRefreshes.set(refreshToken, refresh);
  void refresh.finally(() => inFlightRefreshes.delete(refreshToken)).catch(() => undefined);
  return refresh;
}
