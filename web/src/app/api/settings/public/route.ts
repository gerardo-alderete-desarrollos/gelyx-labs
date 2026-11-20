import { apiFetch } from "@/lib/api/client";

export async function GET() {
  const response = await apiFetch("settings/public");
  return new Response(response.body, {
    status: response.status,
    statusText: response.statusText,
    headers: response.headers,
  });
}
