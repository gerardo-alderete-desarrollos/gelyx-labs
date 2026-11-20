import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { RoleFormDialog } from "@/features/roles/components/role-form-dialog";
import { RolesRequestError } from "@/features/roles/services/roles.service";
import type { Role } from "@/features/roles/schemas/roles.schema";

const roles: Role[] = [
  {
    id: "1f44e8d1-4a1f-4cb0-80c2-0e5e1933d412",
    name: "Soporte",
    description: null,
    isSystem: true,
    userCount: 0,
    permissionCount: 2,
  },
];

afterEach(cleanup);

describe("RoleFormDialog", () => {
  it("validates the name and submits a selected permission-copy source", async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(<RoleFormDialog roles={roles} onClose={vi.fn()} onSubmit={onSubmit} />);
    const user = userEvent.setup();

    await user.click(screen.getByRole("button", { name: "Guardar" }));
    expect(await screen.findByText("El nombre es obligatorio.")).toBeInTheDocument();

    await user.type(screen.getByLabelText("Nombre"), "Supervisor");
    await user.selectOptions(screen.getByLabelText("Copiar permisos de..."), roles[0].id);
    await user.click(screen.getByRole("button", { name: "Guardar" }));
    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith({
      name: "Supervisor",
      description: "",
      copyPermissionsFromRoleId: roles[0].id,
    }));
  });

  it("maps API validation errors to the matching form field", async () => {
    const onSubmit = vi.fn().mockRejectedValue(new RolesRequestError(
      "El nombre ya existe.",
      422,
      "role_name_taken",
      { name: ["El nombre ya existe."] },
    ));
    const customRole = { ...roles[0], name: "Supervisor", isSystem: false };
    render(<RoleFormDialog roles={roles} role={customRole} onClose={vi.fn()} onSubmit={onSubmit} />);

    expect(screen.getByLabelText("Nombre")).toBeEnabled();
    await userEvent.setup().click(screen.getByRole("button", { name: "Guardar" }));
    expect(await screen.findByText("Ya existe un rol con ese nombre.")).toBeInTheDocument();
  });

  it("disables the name for a base role", async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(<RoleFormDialog roles={roles} role={roles[0]} onClose={vi.fn()} onSubmit={onSubmit} />);

    expect(screen.getByLabelText("Nombre")).toBeDisabled();
    await userEvent.setup().click(screen.getByRole("button", { name: "Guardar" }));
    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith({
      name: "Soporte",
      description: "",
      copyPermissionsFromRoleId: undefined,
    }));
  });
});
