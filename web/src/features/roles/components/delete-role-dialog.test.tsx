import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { DeleteRoleDialog } from "@/features/roles/components/delete-role-dialog";
import type { Role } from "@/features/roles/schemas/roles.schema";

const assignedRole: Role = {
  id: "1f44e8d1-4a1f-4cb0-80c2-0e5e1933d412",
  name: "Supervisor",
  description: null,
  isSystem: false,
  userCount: 2,
  permissionCount: 1,
};

const destinationRole: Role = {
  id: "a7869042-72de-4731-a725-31ac53e08f5f",
  name: "Usuario",
  description: null,
  isSystem: true,
  userCount: 0,
  permissionCount: 0,
};

describe("DeleteRoleDialog", () => {
  it("requires a destination role before confirming user reassignment", async () => {
    const onConfirm = vi.fn().mockResolvedValue(undefined);
    render(<DeleteRoleDialog role={assignedRole} roles={[assignedRole, destinationRole]} onClose={vi.fn()} onConfirm={onConfirm} />);
    const user = userEvent.setup();
    const confirm = screen.getByRole("button", { name: "Eliminar" });

    expect(screen.getByText(/Hay 2 usuario\(s\) asignado\(s\)/)).toBeInTheDocument();
    expect(confirm).toBeDisabled();
    await user.selectOptions(screen.getByLabelText("Reasignar usuarios a"), destinationRole.id);
    expect(confirm).toBeEnabled();
    await user.click(confirm);
    expect(onConfirm).toHaveBeenCalledWith(destinationRole.id);
  });
});
