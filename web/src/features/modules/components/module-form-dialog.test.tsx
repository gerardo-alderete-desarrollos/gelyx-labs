import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { ModuleFormDialog } from "@/features/modules/components/module-form-dialog";
import type { ManagedModule } from "@/features/modules/schemas/modules.schema";

const managedModule: ManagedModule = {
  id: "3f0d0e19-455a-4a9a-b5fb-59b45220c963",
  key: "products",
  icon: "package",
  route: "/dashboard/products",
  sortOrder: 10,
  isActive: true,
  isSystem: false,
  translations: [
    { locale: "es", name: "Productos", description: "Gestiona productos" },
    { locale: "en", name: "Products", description: "Manage products" },
  ],
  roles: [{ id: "support", name: "Soporte" }],
};

afterEach(cleanup);

describe("ModuleFormDialog", () => {
  it("edits every supported locale, an icon-map icon, and the numeric order", async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(<ModuleFormDialog module={managedModule} onClose={vi.fn()} onSubmit={onSubmit} />);
    const user = userEvent.setup();

    await user.clear(screen.getByLabelText("Nombre (Español)"));
    await user.type(screen.getByLabelText("Nombre (Español)"), "Catálogo");
    await user.selectOptions(screen.getByLabelText("Icono"), "box");
    await user.clear(screen.getByLabelText("Orden"));
    await user.type(screen.getByLabelText("Orden"), "3");
    await user.click(screen.getByRole("button", { name: "Guardar" }));

    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith({
      icon: "box",
      sortOrder: 3,
      translations: [
        { locale: "es", name: "Catálogo", description: "Gestiona productos" },
        { locale: "en", name: "Products", description: "Manage products" },
      ],
    }));
  });

  it("requires a localized name before submitting", async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(<ModuleFormDialog module={managedModule} onClose={vi.fn()} onSubmit={onSubmit} />);
    const user = userEvent.setup();

    await user.clear(screen.getByLabelText("Nombre (Inglés)"));
    await user.click(screen.getByRole("button", { name: "Guardar" }));

    expect(await screen.findByText("El nombre del módulo es obligatorio.")).toBeInTheDocument();
    expect(onSubmit).not.toHaveBeenCalled();
  });
});
