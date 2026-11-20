import { beforeEach, describe, expect, it, vi } from "vitest";
import { fetchBff } from "@/lib/api/browser-client";
import { modulesService } from "@/features/modules/services/modules.service";

vi.mock("@/lib/api/browser-client", () => ({
  fetchBff: vi.fn(),
}));

const moduleDetails = {
  id: "3f0d0e19-455a-4a9a-b5fb-59b45220c963",
  key: "products",
  translations: {
    es: { name: "Productos", description: "Gestiona productos" },
    en: { name: "Products", description: "Manage products" },
  },
  icon: "package",
  route: "/dashboard/products",
  sortOrder: 10,
  isSystem: false,
  isActive: true,
  permissions: [],
  rolesWithAccess: [{ id: "88e96e55-1b0a-4fb7-a503-062bbefc4d7d", name: "Soporte" }],
};

describe("modules service", () => {
  beforeEach(() => vi.clearAllMocks());

  it("maps the API module details contract into the UI shape", async () => {
    vi.mocked(fetchBff).mockResolvedValue(new Response(JSON.stringify([moduleDetails])));

    await expect(modulesService.list()).resolves.toEqual([{
      id: moduleDetails.id,
      key: moduleDetails.key,
      icon: moduleDetails.icon,
      route: moduleDetails.route,
      sortOrder: moduleDetails.sortOrder,
      isSystem: moduleDetails.isSystem,
      isActive: moduleDetails.isActive,
      translations: [
        { locale: "es", name: "Productos", description: "Gestiona productos" },
        { locale: "en", name: "Products", description: "Manage products" },
      ],
      roles: moduleDetails.rolesWithAccess,
    }]);
  });

  it("uses module IDs, dictionary translations, and the status endpoint", async () => {
    vi.mocked(fetchBff)
      .mockResolvedValueOnce(new Response(JSON.stringify(moduleDetails)))
      .mockResolvedValueOnce(new Response(null, { status: 204 }));

    await modulesService.update(moduleDetails.id, {
      icon: "box",
      sortOrder: 3,
      translations: [
        { locale: "es", name: "Catálogo", description: "Gestiona productos" },
        { locale: "en", name: "Products", description: "Manage products" },
      ],
    });
    await expect(modulesService.updateState(moduleDetails.id, false)).resolves.toBeUndefined();

    expect(fetchBff).toHaveBeenNthCalledWith(1, `/api/modules/${moduleDetails.id}`, {
      method: "PUT",
      body: JSON.stringify({
        icon: "box",
        sortOrder: 3,
        translations: {
          es: { name: "Catálogo", description: "Gestiona productos" },
          en: { name: "Products", description: "Manage products" },
        },
      }),
    });
    expect(fetchBff).toHaveBeenNthCalledWith(2, `/api/modules/${moduleDetails.id}/status`, {
      method: "PATCH",
      body: JSON.stringify({ isActive: false }),
    });
  });
});
