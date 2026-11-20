import { proxyAuthenticatedRequest } from "@/lib/auth/proxy-request";

export function GET(request: Request) {
  return proxyAuthenticatedRequest(request, "audit-logs/export");
}
