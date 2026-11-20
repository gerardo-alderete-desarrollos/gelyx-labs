import "server-only";

import { expiredSessionResponse, responseFromApi } from "@/lib/auth/response";
import { sessionApiRequest } from "@/lib/auth/session-api";

export async function proxyAuthenticatedRequest(request: Request, path: string): Promise<Response> {
  const url = new URL(request.url);
  const body = ["GET", "HEAD"].includes(request.method) ? undefined : await request.text();
  const result = await sessionApiRequest(`${path}${url.search}`, {
    method: request.method,
    body,
  });

  if (result.expired) {
    return expiredSessionResponse();
  }

  return responseFromApi(result.response, result.tokens);
}
