import { expect, test } from "@playwright/test";

const adminEmail = process.env.E2E_ADMIN_EMAIL;
const adminPassword = process.env.E2E_ADMIN_PASSWORD;

test("El idioma del perfil se conserva después de recargar", async ({ page }) => {
  test.skip(!adminEmail || !adminPassword, "Configura E2E_ADMIN_EMAIL y E2E_ADMIN_PASSWORD para ejecutar el flujo integrado.");

  await page.goto("/login");
  await page.getByLabel("Correo electrónico").fill(adminEmail!);
  await page.getByLabel("Contraseña").fill(adminPassword!);
  await page.getByRole("button", { name: "Iniciar sesión" }).click();
  await page.goto("/dashboard/profile");
  await page.getByRole("tab", { name: "Preferencias" }).click();
  await page.getByLabel("Idioma").selectOption("en");
  await page.getByRole("button", { name: "Guardar preferencias" }).click();
  await expect(page.getByRole("tab", { name: "Preferences" })).toBeVisible();

  await page.reload();
  await expect(page.getByRole("tab", { name: "Preferences" })).toBeVisible();

  await page.getByLabel("Language").selectOption("es");
  await page.getByRole("button", { name: "Save preferences" }).click();
});
