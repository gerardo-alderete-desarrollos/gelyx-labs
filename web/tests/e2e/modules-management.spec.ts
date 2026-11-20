import { expect, test } from "@playwright/test";

const adminEmail = process.env.E2E_ADMIN_EMAIL;
const adminPassword = process.env.E2E_ADMIN_PASSWORD;
const supportEmail = process.env.E2E_SUPPORT_EMAIL;
const supportPassword = process.env.E2E_SUPPORT_PASSWORD;

test("Admin updates a module and Soporte cannot open module management", async ({ page, browser, baseURL }) => {
  test.skip(!adminEmail || !adminPassword || !supportEmail || !supportPassword, "Configure E2E_ADMIN_* and E2E_SUPPORT_* credentials to run this integrated flow.");

  await signIn(page, adminEmail!, adminPassword!);
  await page.goto("/dashboard/modules");
  const dashboardRow = page.locator("tbody tr").filter({ hasText: "dashboard" });
  await expect(dashboardRow).toBeVisible();
  const originalName = await dashboardRow.locator("td").first().innerText();
  const updatedName = `Panel e2e ${Date.now()}`;

  await dashboardRow.getByRole("button", { name: "Editar" }).click();
  const dialog = page.getByRole("dialog", { name: "Editar módulo: dashboard" });
  const originalOrder = await dialog.getByLabel("Orden").inputValue();
  await dialog.getByLabel("Nombre (Español)").fill(updatedName);
  await dialog.getByLabel("Orden").fill("0");
  await dialog.getByRole("button", { name: "Guardar" }).click();
  await expect(page.getByRole("link", { name: updatedName, exact: true })).toBeVisible();

  await dashboardRow.getByRole("button", { name: "Editar" }).click();
  const restoreDialog = page.getByRole("dialog", { name: "Editar módulo: dashboard" });
  await restoreDialog.getByLabel("Nombre (Español)").fill(originalName);
  await restoreDialog.getByLabel("Orden").fill(originalOrder);
  await restoreDialog.getByRole("button", { name: "Guardar" }).click();

  const supportContext = await browser.newContext({ baseURL });
  const supportPage = await supportContext.newPage();
  await signIn(supportPage, supportEmail!, supportPassword!);
  await supportPage.goto("/dashboard/modules");
  await expect(supportPage).toHaveURL(/\/403$/);
  await supportContext.close();
});

async function signIn(page: import("@playwright/test").Page, email: string, password: string) {
  await page.goto("/login");
  await page.getByLabel("Correo electrónico").fill(email);
  await page.getByLabel("Contraseña").fill(password);
  await page.getByRole("button", { name: "Iniciar sesión" }).click();
  await expect(page).toHaveURL(/\/dashboard(?:$|\/)/);
}
