import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { ExportCsvButton } from "@/components/export-csv-button";

const { fetchBff, notifyError } = vi.hoisted(() => ({
  fetchBff: vi.fn(),
  notifyError: vi.fn(),
}));

vi.mock("@/lib/api/browser-client", () => ({ fetchBff }));
vi.mock("@/lib/notify", () => ({ notify: { error: notifyError } }));

describe("ExportCsvButton", () => {
  it("downloads the CSV returned by the BFF route", async () => {
    fetchBff.mockResolvedValue(new Response("id\n1\n", {
      headers: { "content-disposition": "attachment; filename*=UTF-8''filtered-users.csv" },
    }));
    const createObjectUrl = vi.fn(() => "blob:export");
    const revokeObjectUrl = vi.fn();
    vi.stubGlobal("URL", { createObjectURL: createObjectUrl, revokeObjectURL: revokeObjectUrl });
    const click = vi.spyOn(HTMLAnchorElement.prototype, "click").mockImplementation(() => {});

    render(<ExportCsvButton path="/api/users/export?status=active" label="Exportar" loadingLabel="Exportando" errorMessage="Error" fallbackFileName="users.csv" />);
    await userEvent.setup().click(screen.getByRole("button", { name: "Exportar" }));

    await waitFor(() => expect(fetchBff).toHaveBeenCalledWith("/api/users/export?status=active"));
    expect(click).toHaveBeenCalledOnce();
    expect(createObjectUrl).toHaveBeenCalledOnce();
    expect(revokeObjectUrl).toHaveBeenCalledWith("blob:export");

    click.mockRestore();
    vi.unstubAllGlobals();
  });
});
