import { proxyAuthenticatedRequest } from "@/lib/auth/proxy-request";

type RouteContext = Readonly<{ params: Promise<{ id: string }> }>;

export async function PUT(request: Request, { params }: RouteContext) {
  const { id } = await params;
  return proxyAuthenticatedRequest(request, `roles/${id}`);
}

export async function DELETE(request: Request, { params }: RouteContext) {
  const { id } = await params;
  return proxyAuthenticatedRequest(request, `roles/${id}`);
}
