import { expect, test } from "@playwright/test";

test("allows showing a password and preloads the remembered email", async ({ page }) => {
  await page.addInitScript(() => {
    localStorage.setItem("auth.rememberedEmail", "person@example.com");
  });
  await page.goto("/login");

  const password = page.getByLabel("Contraseña");
  await expect(password).toHaveAttribute("type", "password");
  await page.getByRole("button", { name: "Mostrar contraseña" }).click();
  await expect(password).toHaveAttribute("type", "text");
  await expect(page.getByRole("button", { name: "Ocultar contraseña" })).toHaveAttribute("aria-pressed", "true");
  await expect(page.getByLabel("Correo electrónico")).toHaveValue("person@example.com");
  await expect(page.getByLabel("Recordarme")).toBeChecked();
});
