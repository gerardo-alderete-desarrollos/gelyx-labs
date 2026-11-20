import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { LoginForm } from "@/features/auth/components/login-form";
import { login, type LoginResult } from "@/features/auth/services/login";

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace: vi.fn(), refresh: vi.fn() }),
}));

vi.mock("@/features/auth/services/login", () => ({
  login: vi.fn(),
}));

describe("LoginForm", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    if (typeof window !== "undefined") {
      localStorage.clear();
    }
  });

  it("asks for a second factor when the API requires it", async () => {
    vi.mocked(login).mockResolvedValue({ twoFactorRequired: true, twoFactorToken: "temporary-token" });
    const queryClient = new QueryClient();
    render(<QueryClientProvider client={queryClient}><LoginForm returnUrl={null} reason={null} /></QueryClientProvider>);
    fireEvent.change(screen.getByLabelText("Correo electrónico"), { target: { value: "test@example.com" } });
    fireEvent.change(screen.getByLabelText("Contraseña"), { target: { value: "Password123!" } });
    fireEvent.click(screen.getByRole("button", { name: "Iniciar sesión" }));
    await waitFor(() => expect(screen.getByLabelText("Código de autenticación")).toBeInTheDocument());
  });

  it("validates required email and password before submitting", async () => {
    const queryClient = new QueryClient();
    render(
      <QueryClientProvider client={queryClient}>
        <LoginForm returnUrl={null} reason={null} />
      </QueryClientProvider>,
    );
    fireEvent.click(screen.getByRole("button", { name: "Iniciar sesión" }));

    await waitFor(() => {
      expect(screen.getByText("Ingresa un correo válido.")).toBeInTheDocument();
      expect(screen.getByText("Ingresa tu contraseña.")).toBeInTheDocument();
    });
    expect(login).not.toHaveBeenCalled();
  });

  it("saves email to localStorage when rememberMe is checked and preloads on mount", async () => {
    vi.mocked(login).mockResolvedValue({} as LoginResult);
    const queryClient = new QueryClient();
    render(
      <QueryClientProvider client={queryClient}>
        <LoginForm returnUrl={null} reason={null} />
      </QueryClientProvider>,
    );

    expect(screen.getByLabelText("Correo electrónico")).toHaveValue("");

    fireEvent.change(screen.getByLabelText("Correo electrónico"), { target: { value: "remember@example.com" } });
    fireEvent.change(screen.getByLabelText("Contraseña"), { target: { value: "Password123!" } });
    fireEvent.click(screen.getByLabelText("Recordarme"));
    fireEvent.click(screen.getByRole("button", { name: "Iniciar sesión" }));

    await waitFor(() => expect(login).toHaveBeenCalledTimes(1));
    expect(localStorage.getItem("auth.rememberedEmail")).toBe("remember@example.com");

    // Remount and verify email is preloaded
    localStorage.setItem("auth.rememberedEmail", "remember@example.com");
    render(
      <QueryClientProvider client={queryClient}>
        <LoginForm returnUrl={null} reason={null} />
      </QueryClientProvider>,
    );
    expect(screen.getByLabelText("Correo electrónico")).toHaveValue("remember@example.com");
    expect(screen.getByLabelText("Recordarme")).toBeChecked();
  });

  it("clears remembered email when rememberMe is unchecked", async () => {
    vi.mocked(login).mockResolvedValue({} as LoginResult);
    const queryClient = new QueryClient();
    localStorage.setItem("auth.rememberedEmail", "clear@example.com");
    render(
      <QueryClientProvider client={queryClient}>
        <LoginForm returnUrl={null} reason={null} />
      </QueryClientProvider>,
    );

    expect(screen.getByLabelText("Correo electrónico")).toHaveValue("clear@example.com");
    expect(screen.getByLabelText("Recordarme")).toBeChecked();

    fireEvent.change(screen.getByLabelText("Contraseña"), { target: { value: "Password123!" } });
    fireEvent.click(screen.getByLabelText("Recordarme"));
    expect(screen.getByLabelText("Recordarme")).not.toBeChecked();
    fireEvent.click(screen.getByRole("button", { name: "Iniciar sesión" }));

    await waitFor(() => expect(login).toHaveBeenCalledTimes(1));
    expect(localStorage.getItem("auth.rememberedEmail")).toBeNull();
  });

  it("never stores password in localStorage or sessionStorage", async () => {
    vi.mocked(login).mockResolvedValue({} as LoginResult);
    const queryClient = new QueryClient();
    render(
      <QueryClientProvider client={queryClient}>
        <LoginForm returnUrl={null} reason={null} />
      </QueryClientProvider>,
    );

    fireEvent.change(screen.getByLabelText("Correo electrónico"), { target: { value: "test@example.com" } });
    fireEvent.change(screen.getByLabelText("Contraseña"), { target: { value: "SecretPassword123!" } });
    fireEvent.click(screen.getByLabelText("Recordarme"));
    fireEvent.click(screen.getByRole("button", { name: "Iniciar sesión" }));

    await waitFor(() => expect(login).toHaveBeenCalledTimes(1));

    expect(localStorage.getItem("auth.rememberedEmail")).toBe("test@example.com");
    expect(JSON.stringify(localStorage)).not.toContain("SecretPassword123!");
    expect(JSON.stringify(sessionStorage)).not.toContain("SecretPassword123!");
  });
});
