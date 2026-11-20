import { proxyAuthenticatedRequest } from "@/lib/auth/proxy-request";

type RouteContext = Readonly<{ params: Promise<{ path: string[] }> }>;

export async function PUT(request: Request, { params }: RouteContext) {
  const { path } = await params;
  return proxyAuthenticatedRequest(request, `modules/${path.join("/")}`);
}

export async function PATCH(request: Request, { params }: RouteContext) {
  const { path } = await params;
  return proxyAuthenticatedRequest(request, `modules/${path.join("/")}`);
}
