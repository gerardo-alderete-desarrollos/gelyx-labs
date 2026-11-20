import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { UserFormDialog } from "@/features/users/components/user-form-dialog";

const roles = [
  { id: "1f44e8d1-4a1f-4cb0-80c2-0e5e1933d412", name: "Soporte" },
];

describe("UserFormDialog", () => {
  it("shows validation messages and submits selected roles", async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(<UserFormDialog roles={roles} onClose={vi.fn()} onSubmit={onSubmit} />);
    const user = userEvent.setup();

    await user.click(screen.getByRole("button", { name: "Crear usuario" }));
    expect(await screen.findByText("El nombre es obligatorio.")).toBeInTheDocument();
    expect(screen.getByText("Selecciona al menos un rol.")).toBeInTheDocument();

    await user.type(screen.getByLabelText("Nombre completo"), "Ana López");
    await user.type(screen.getByLabelText("Correo electrónico"), "ana@example.test");
    await user.click(screen.getByLabelText("Soporte"));
    await user.click(screen.getByRole("button", { name: "Crear usuario" }));

    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith({
      fullName: "Ana López",
      email: "ana@example.test",
      roleIds: [roles[0].id],
    }));
  });
});
