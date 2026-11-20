import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { PreferencesForm } from "@/features/profile/components/preferences-form";

const { setTheme, refresh, fetchBff } = vi.hoisted(() => ({
  setTheme: vi.fn(),
  refresh: vi.fn(),
  fetchBff: vi.fn(),
}));

vi.mock("next-themes", () => ({ useTheme: () => ({ setTheme }) }));
vi.mock("next/navigation", () => ({ useRouter: () => ({ refresh }) }));
vi.mock("@/features/auth/hooks/use-session", () => ({
  sessionQueryKey: ["auth", "me"],
  useSession: () => ({ data: { locale: "es", theme: "system" } }),
}));
vi.mock("@/lib/api/browser-client", () => ({ fetchBff }));
vi.mock("@/lib/notify", () => ({ notify: { success: vi.fn(), error: vi.fn() } }));

afterEach(() => {
  vi.clearAllMocks();
  document.cookie = "NEXT_LOCALE=; Path=/; Max-Age=0";
});

describe("PreferencesForm", () => {
  it("saves language and theme, applies them, and refreshes for a locale change", async () => {
    fetchBff.mockResolvedValue(new Response(null, { status: 204 }));
    const user = userEvent.setup();
    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    render(
      <QueryClientProvider client={queryClient}>
        <PreferencesForm />
      </QueryClientProvider>,
    );

    await user.selectOptions(screen.getByLabelText("Idioma"), "en");
    await user.selectOptions(screen.getByLabelText("Tema"), "dark");
    await user.click(screen.getByRole("button", { name: "Guardar preferencias" }));

    await waitFor(() => expect(fetchBff).toHaveBeenCalledWith("/api/auth/me/preferences", {
      method: "PUT",
      body: JSON.stringify({ locale: "en", theme: "dark" }),
    }));
    await waitFor(() => expect(setTheme).toHaveBeenCalledWith("dark"));
    expect(document.cookie).toContain("NEXT_LOCALE=en");
    expect(refresh).toHaveBeenCalled();
  });
});
