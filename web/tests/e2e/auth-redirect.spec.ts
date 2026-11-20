import { expect, test } from "@playwright/test";

test("una sesión anónima es redirigida al inicio de sesión al abrir el panel", async ({ page }) => {
  await page.goto("/dashboard");
  await expect(page).toHaveURL(/\/login\?returnUrl=%2Fdashboard/);
  await expect(page.getByRole("heading", { name: "Inicia sesión" })).toBeVisible();
});
