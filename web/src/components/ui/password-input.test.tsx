import { fireEvent, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { PasswordInput } from "@/components/ui/password-input";

describe("PasswordInput", () => {
  it("toggles visibility accessibly without submitting its form and hides on submit", async () => {
    const onSubmit = vi.fn((event: React.FormEvent<HTMLFormElement>) => event.preventDefault());
    const user = userEvent.setup();
    render(
      <form onSubmit={onSubmit}>
        <PasswordInput aria-label="Contraseña" />
        <button type="submit">Enviar</button>
      </form>,
    );

    const input = screen.getByLabelText("Contraseña");
    const toggle = screen.getByRole("button", { name: "Mostrar contraseña" });
    expect(input).toHaveAttribute("type", "password");
    expect(toggle).toHaveAttribute("aria-pressed", "false");

    await user.click(toggle);
    expect(onSubmit).not.toHaveBeenCalled();
    expect(input).toHaveAttribute("type", "text");
    expect(screen.getByRole("button", { name: "Ocultar contraseña" })).toHaveAttribute("aria-pressed", "true");

    fireEvent.submit(input.closest("form")!);
    expect(onSubmit).toHaveBeenCalledTimes(1);
    expect(input).toHaveAttribute("type", "password");
  });

  it("resets to hidden when unmounted", () => {
    const { unmount } = render(<PasswordInput aria-label="Contraseña" />);
    const toggle = screen.getByRole("button", { name: "Mostrar contraseña" });
    fireEvent.click(toggle);
    expect(screen.getByRole("button", { name: "Ocultar contraseña" })).toBeInTheDocument();
    unmount();
    // Should not throw
  });
});
