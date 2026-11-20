import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { PermissionsMatrix } from "@/features/permissions/components/permissions-matrix";
import { getPermissionsMatrix } from "@/features/permissions/services/permissions.service";

vi.mock("@/features/permissions/services/permissions.service", () => ({
  getPermissionsMatrix: vi.fn(),
  saveRolePermissions: vi.fn(),
}));
vi.mock("@/features/auth/hooks/use-session", () => ({
  useSession: () => ({ data: { permissions: ["permissions.manage"] } }),
}));

const matrix = {
  roles: [
    { id: "c45612e5-b49a-4a10-9d9e-9520103691f6", name: "Admin", isSystem: true, modules: [
      { key: "dashboard", permissions: [{ key: "dashboard.access", name: "Acceso", granted: true }, { key: "dashboard.read", name: "Leer", granted: true }] },
      { key: "users", permissions: [{ key: "users.access", name: "Acceso", granted: true }, { key: "users.read", name: "Leer", granted: true }] },
    ] },
    { id: "113218ad-f5de-47fc-86c8-3b73bf193bb4", name: "Soporte", isSystem: true, modules: [
      { key: "dashboard", permissions: [{ key: "dashboard.access", name: "Acceso", granted: true }, { key: "dashboard.read", name: "Leer", granted: true }] },
      { key: "users", permissions: [{ key: "users.access", name: "Acceso", granted: false }, { key: "users.read", name: "Leer", granted: false }] },
    ] },
  ],
  modules: [
    { key: "dashboard", name: "Panel", isSystem: false, permissions: [{ key: "dashboard.access", name: "Acceso" }, { key: "dashboard.read", name: "Leer" }] },
    { key: "users", name: "Usuarios", isSystem: true, permissions: [{ key: "users.access", name: "Acceso" }, { key: "users.read", name: "Leer" }] },
  ],
};

function renderMatrix() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(<QueryClientProvider client={client}><PermissionsMatrix /></QueryClientProvider>);
}

describe("PermissionsMatrix", () => {
  beforeEach(() => vi.mocked(getPermissionsMatrix).mockResolvedValue(matrix));

  it("keeps Admin locked and blocks system-module changes for other roles", async () => {
    renderMatrix();
    const adminDashboard = await screen.findByRole("checkbox", { name: "Panel — Admin" });
    const supportUsers = screen.getByRole("checkbox", { name: "Usuarios — Soporte" });
    expect(adminDashboard).toBeChecked();
    expect(adminDashboard).toBeDisabled();
    expect(supportUsers).toBeDisabled();
  });

  it("removing module access also removes its action permissions", async () => {
    renderMatrix();
    fireEvent.click(await screen.findByRole("button", { name: "+ Panel" }));
    const supportAccess = screen.getByRole("checkbox", { name: "Panel — Soporte" });
    const supportRead = screen.getByRole("checkbox", { name: "Leer — Soporte" });
    expect(supportRead).toBeChecked();
    fireEvent.click(supportAccess);
    await waitFor(() => expect(supportRead).not.toBeChecked());
  });
});
