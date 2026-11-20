import "server-only";

import { redirect } from "next/navigation";
import { cookies } from "next/headers";
import { apiFetch } from "@/lib/api/client";
import { ACCESS_TOKEN_COOKIE, REFRESH_TOKEN_COOKIE } from "@/lib/auth/cookies";

export async function fetchServerApi(path: string, init: RequestInit = {}): Promise<Response> {
  const cookieStore = await cookies();
  const accessToken = cookieStore.get(ACCESS_TOKEN_COOKIE)?.value;
  if (!accessToken) {
    redirect("/api/auth/refresh?returnUrl=%2Fdashboard");
  }

  const response = await apiFetch(path, accessToken, init);
  if (response.status === 401) {
    const returnUrl = `/api/auth/refresh?returnUrl=${encodeURIComponent("/dashboard")}`;
    if (cookieStore.get(REFRESH_TOKEN_COOKIE)) {
      redirect(returnUrl);
    }
    redirect(`/login?reason=session-expired&returnUrl=${encodeURIComponent("/dashboard")}`);
  }

  return response;
}
