import { expect, test } from "@playwright/test";

const adminEmail = process.env.E2E_ADMIN_EMAIL;
const adminPassword = process.env.E2E_ADMIN_PASSWORD;

test("keeps the app header and changes bar above a scrolling permissions matrix", async ({ page }, testInfo) => {
  test.setTimeout(120_000);
  test.skip(!adminEmail || !adminPassword, "Configura E2E_ADMIN_EMAIL y E2E_ADMIN_PASSWORD para ejecutar el flujo integrado.");

  await signIn(page, adminEmail!, adminPassword!);
  await page.goto("/dashboard/permissions");
  await expect(page.getByTestId("permissions-matrix-scroll")).toBeVisible();

  const editablePermission = page.locator('tbody input[type="checkbox"]:not(:disabled)').first();
  await editablePermission.click();
  await expect(page.getByTestId("permissions-changes-bar")).toBeVisible();

  const matrix = page.getByTestId("permissions-matrix-scroll");
  await matrix.evaluate((element) => {
    element.scrollTop = element.scrollHeight;
    element.scrollLeft = element.scrollWidth;
  });

  await expect.poll(() => page.evaluate(() => {
    const header = document.querySelector("header");
    if (!header) return false;
    const rect = header.getBoundingClientRect();
    return document.elementFromPoint(window.innerWidth / 2, rect.top + (rect.height / 2))?.closest("header") === header;
  })).toBe(true);

  await page.screenshot({ path: testInfo.outputPath("permissions-layout-desktop-light.png") });
  await expect(page.getByRole("button", { name: "Descartar" })).toBeEnabled();
  await page.getByRole("button", { name: "Descartar" }).click();
  await expect(page.getByTestId("permissions-changes-bar")).toHaveCount(0);

  await editablePermission.click();
  await page.setViewportSize({ width: 375, height: 812 });
  await page.evaluate(() => document.documentElement.classList.add("dark"));
  await page.screenshot({ path: testInfo.outputPath("permissions-layout-mobile-dark.png") });
  await expect(page.getByRole("button", { name: "Guardar cambios" })).toBeEnabled();
  await page.getByRole("button", { name: "Descartar" }).click();
});

async function signIn(page: import("@playwright/test").Page, email: string, password: string) {
  await page.goto("/login");
  await page.waitForLoadState("networkidle");
  await page.getByLabel("Correo electrónico").fill(email);
  await page.getByRole("textbox", { name: "Contraseña" }).fill(password);
  await page.getByRole("button", { name: "Iniciar sesión" }).click();
  await page.waitForURL(/\/(?:change-password|dashboard)(?:$|\/)/, { timeout: 30000 });
  if (new URL(page.url()).pathname === "/change-password") {
    const nextPassword = `E2eAdmin-${Date.now()}Aa1!`;
    await page.getByLabel("Contraseña actual").fill(password);
    await page.getByLabel("Nueva contraseña", { exact: true }).fill(nextPassword);
    await page.getByLabel("Confirma la nueva contraseña", { exact: true }).fill(nextPassword);
    await page.getByRole("button", { name: "Cambiar contraseña" }).click();
    await expect(page).toHaveURL(/\/login\?reason=password-changed/);
    await signIn(page, email, nextPassword);
  }
}
