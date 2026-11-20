import { expect, test, type Browser, type BrowserContext, type Page } from "@playwright/test";

const adminEmail = process.env.E2E_ADMIN_EMAIL;
const adminPassword = process.env.E2E_ADMIN_PASSWORD;
const apiBaseUrl = (process.env.API_BASE_URL ?? "http://localhost:5000/api/v1").replace(/\/+$/, "");

type Tokens = { accessToken: string; refreshToken: string };

test("Admin administra usuarios, roles y permisos; Soporte y Usuario quedan fuera de módulos de sistema", async ({ page, context, browser, baseURL }) => {
  test.setTimeout(360_000);
  test.skip(!adminEmail || !adminPassword, "Configura E2E_ADMIN_EMAIL y E2E_ADMIN_PASSWORD para ejecutar el flujo integrado.");
  const stamp = `${Date.now()}-${Math.random().toString(16).slice(2, 8)}`;
  const apiOrigin = new URL(process.env.API_BASE_URL ?? "http://localhost:5000/api/v1").origin;
  const directApiRequests: string[] = [];
  page.on("request", (request) => {
    if (new URL(request.url()).origin === apiOrigin) directApiRequests.push(request.url());
  });
  await signInAsAdmin(page, adminEmail!, adminPassword!);
  await expect(page.getByRole("link", { name: "Usuarios", exact: true })).toBeVisible();
  await expect(page.getByRole("link", { name: "Roles", exact: true })).toBeVisible();
  await expect(page.getByRole("link", { name: "Permisos", exact: true })).toBeVisible();
  await expect(page.getByRole("link", { name: "Auditoría", exact: true })).toBeVisible();

  await page.goto("/dashboard/users");
  const created = await createUser(page, `prompt8-target-${stamp}@example.test`, "Usuario");
  await filterForEmail(page, created.email);
  const targetRow = page.locator("tbody tr").filter({ hasText: created.email });
  await expect(targetRow).toBeVisible();

  await targetRow.getByRole("button", { name: "Editar" }).click();
  const editDialog = page.getByRole("dialog", { name: "Editar usuario" });
  await editDialog.getByLabel("Nombre completo").fill("Usuario Prompt 8 Editado");
  await editDialog.getByRole("button", { name: "Guardar" }).click();
  await expect(targetRow.getByText("Usuario Prompt 8 Editado")).toBeVisible();

  await targetRow.getByRole("button", { name: "Desactivar" }).click();
  await page.getByRole("alertdialog").getByRole("button", { name: "Desactivar" }).click();
  await expect(targetRow.getByText("Inactivo")).toBeVisible();
  await targetRow.getByRole("button", { name: "Activar" }).click();
  await page.getByRole("alertdialog").getByRole("button", { name: "Activar" }).click();
  await expect(targetRow.getByText("Activo")).toBeVisible();

  await targetRow.getByRole("button", { name: "Eliminar" }).click();
  await page.getByRole("alertdialog").getByRole("button", { name: "Eliminar" }).click();
  await page.getByLabel("Filtrar por estado").selectOption("deleted");
  await expect(page.locator("tbody tr").filter({ hasText: created.email })).toBeVisible();
  await page.locator("tbody tr").filter({ hasText: created.email }).getByRole("button", { name: "Restaurar" }).click();
  await page.getByRole("alertdialog").getByRole("button", { name: "Restaurar" }).click();
  await expect(page.locator("tbody tr").filter({ hasText: created.email }).getByText("Usuario Prompt 8 Editado")).toBeVisible();

  await page.goto("/dashboard/permissions");
  await expect(page.locator("thead")).toContainText("Soporte");
  const moduleRows = page.locator("tbody tr").filter({ has: page.locator("th button") });
  const roleHeaders = await page.locator("thead th").allInnerTexts();
  const supportColumn = roleHeaders.findIndex((header) => header.trim() === "Soporte") - 1;
  expect(supportColumn).toBeGreaterThanOrEqual(0);
  const count = await moduleRows.count();
  let changed = false;
  for (let index = 0; index < count; index += 1) {
    const row = moduleRows.nth(index);
    const label = await row.locator("th").innerText();
    const checkbox = row.locator('td input[type="checkbox"]').nth(supportColumn);
    if (!label.includes("Sistema") && await checkbox.isEnabled()) {
      await row.locator("th button").click();
      const current = await checkbox.isChecked();
      await checkbox.setChecked(!current);
      await page.getByRole("button", { name: "Guardar cambios" }).click();
      await page.getByRole("alertdialog").getByRole("button", { name: "Confirmar y guardar" }).click();
      await expect(page.getByText("Matriz de permisos actualizada.")).toBeVisible();
      await checkbox.setChecked(current);
      await page.getByRole("button", { name: "Guardar cambios" }).click();
      await page.getByRole("alertdialog").getByRole("button", { name: "Confirmar y guardar" }).click();
      await expect(page.getByText("Matriz de permisos actualizada.").last()).toBeVisible();
      changed = true;
      break;
    }
  }
  expect(changed).toBe(true);

  await page.goto("/dashboard/roles");
  await page.getByRole("button", { name: "Crear rol" }).click();
  const roleDialog = page.getByRole("dialog", { name: "Crear rol" });
  await roleDialog.getByLabel("Nombre del rol").fill("Supervisor");
  await roleDialog.getByLabel("Copiar permisos de").selectOption({ label: "Soporte" });
  await roleDialog.getByRole("button", { name: "Guardar" }).click();
  const supervisorRow = page.locator("tbody tr").filter({ hasText: "Supervisor" });
  await expect(supervisorRow).toBeVisible();
  await supervisorRow.getByRole("link", { name: "Ver permisos" }).click();
  await expect(page).toHaveURL(/\/dashboard\/permissions\?roleId=/);
  const supervisorRoleId = new URL(page.url()).searchParams.get("roleId");
  expect(supervisorRoleId).toBeTruthy();

  const permissionRows = page.locator("tbody tr").filter({ has: page.locator("th button") });
  await expect(page.locator("thead")).toContainText("Supervisor");
  let grantedModuleName = "";
  for (let index = 0; index < await permissionRows.count(); index += 1) {
    const row = permissionRows.nth(index);
    const moduleName = await row.locator("th button").innerText();
    if (await row.locator("th").innerText().then((label) => label.includes("Sistema"))) continue;
    const access = row.locator('input[type="checkbox"][aria-label$=" — Supervisor"]');
    if (await access.isEnabled()) {
      grantedModuleName = moduleName.replace(/^[+−-]\s*/, "").trim();
      if (await access.isChecked()) {
        await access.uncheck();
        await page.getByRole("button", { name: "Guardar cambios" }).click();
        await page.getByRole("alertdialog").getByRole("button", { name: "Confirmar y guardar" }).click();
        await expect(page.getByText("Matriz de permisos actualizada.")).toBeVisible();
      }
      await access.check();
      await page.getByRole("button", { name: "Guardar cambios" }).click();
      await page.getByRole("alertdialog").getByRole("button", { name: "Confirmar y guardar" }).click();
      await expect(page.getByText("Matriz de permisos actualizada.")).toBeVisible();
      break;
    }
  }
  expect(grantedModuleName).not.toBe("");

  await page.goto("/dashboard/users");
  const supervisorUser = await createUser(page, `prompt9-supervisor-${stamp}@example.test`, "Supervisor");
  const supervisorSession = await signInAsTemporaryUser(browser, baseURL!, supervisorUser.email, supervisorUser.temporaryPassword);
  await expect(supervisorSession.page.getByRole("link", { name: grantedModuleName, exact: true })).toBeVisible();
  await supervisorSession.context.close();

  await page.goto("/dashboard/roles");
  const createdSupervisor = page.locator("tbody tr").filter({ hasText: "Supervisor" });
  await createdSupervisor.getByRole("button", { name: "Eliminar" }).click();
  const deleteDialog = page.getByRole("alertdialog", { name: "Eliminar rol" });
  await expect(deleteDialog).toContainText("1 usuario(s) asignado(s)");
  const deleteButton = deleteDialog.getByRole("button", { name: "Eliminar rol" });
  await expect(deleteButton).toBeDisabled();
  await deleteDialog.getByLabel("Reasignar usuarios a").selectOption({ label: "Usuario" });
  await deleteButton.click();
  await expect(createdSupervisor).toHaveCount(0);

  await page.goto("/dashboard/users");
  const support = await createUser(page, `prompt8-support-${stamp}@example.test`, "Soporte");
  const standardUser = await createUser(page, `prompt8-user-${stamp}@example.test`, "Usuario");

  const supportSession = await signInAsTemporaryUser(browser, baseURL!, support.email, support.temporaryPassword);
  const standardSession = await signInAsTemporaryUser(browser, baseURL!, standardUser.email, standardUser.temporaryPassword);
  for (const session of [supportSession, standardSession]) {
    await expect(session.page.getByRole("link", { name: "Panel", exact: true })).toBeVisible();
    await expect(session.page.getByRole("link", { name: "Perfil", exact: true })).toBeVisible();
    for (const linkName of ["Usuarios", "Roles", "Permisos", "Auditoría"]) {
      await expect(session.page.getByRole("link", { name: linkName, exact: true })).toHaveCount(0);
    }
    await session.page.goto("/dashboard/profile");
    await expect(session.page).toHaveURL(/\/dashboard\/profile$/);
    for (const route of ["users", "roles", "permissions", "audit"]) {
      await session.page.goto(`/dashboard/${route}`);
      await expect(session.page).toHaveURL(/\/403$/);
    }
  }

  const legacySupport = await createUser(page, `prompt10-support-${stamp}@example.test`, "Soporte");
  const legacyUser = await createUser(page, `prompt10-user-${stamp}@example.test`, "Usuario");
  await page.goto("/dashboard/users");
  await filterForEmail(page, standardUser.email);
  const activeStandardUser = page.locator("tbody tr").filter({ hasText: standardUser.email });
  await activeStandardUser.getByRole("button", { name: "Desactivar" }).click();
  await page.getByRole("alertdialog").getByRole("button", { name: "Desactivar" }).click();
  await expect(activeStandardUser.getByText("Inactivo")).toBeVisible();
  await standardSession.page.goto("/api/auth/refresh?returnUrl=%2Fdashboard");
  await expect(standardSession.page).toHaveURL(/\/login\?reason=session-expired/);
  await expect(standardSession.page.getByText("Tu sesión expiró. Ingresa nuevamente.")).toBeVisible();
  await standardSession.context.close();

  const otherSupportContext = await browser.newContext({ baseURL });
  const otherSupportPage = await otherSupportContext.newPage();
  await signInWithPassword(otherSupportPage, support.email, supportSession.password);
  const newPassword = `E2ePassword-${Date.now()}Aa1!`;
  await supportSession.page.goto("/change-password");
  await supportSession.page.getByLabel("Contraseña actual").fill(supportSession.password);
  await supportSession.page.getByLabel("Nueva contraseña", { exact: true }).fill(newPassword);
  await supportSession.page.getByLabel("Confirma la nueva contraseña", { exact: true }).fill(newPassword);
  await supportSession.page.getByRole("button", { name: "Cambiar contraseña" }).click();
  await expect(supportSession.page).toHaveURL(/\/login\?reason=password-changed/);
  await otherSupportPage.goto("/api/auth/refresh?returnUrl=%2Fdashboard");
  await expect(otherSupportPage).toHaveURL(/\/login\?reason=session-expired/);
  await expect(otherSupportPage.getByText("Tu sesión expiró. Ingresa nuevamente.")).toBeVisible();
  await supportSession.context.close();
  await otherSupportContext.close();

  const adminPeerPage = await context.newPage();
  await adminPeerPage.goto("/dashboard");
  await adminPeerPage.locator("summary").click();
  await adminPeerPage.getByRole("button", { name: "Cerrar sesión" }).click();
  await expect(adminPeerPage).toHaveURL(/\/login\?reason=signed-out/);
  await expect(page).toHaveURL(/\/login\?reason=signed-out/);
  expect(directApiRequests).toEqual([]);

  await assertSystemModulesDenied(page, context, baseURL!, legacySupport.email, legacySupport.temporaryPassword);
  await assertSystemModulesDenied(page, context, baseURL!, legacyUser.email, legacyUser.temporaryPassword);
});

async function signInAsAdmin(page: Page, email: string, password: string) {
  await page.goto("/login");
  await page.getByLabel("Correo electrónico").fill(email);
  await page.getByLabel("Contraseña").fill(password);
  await page.getByRole("button", { name: "Iniciar sesión" }).click();
  await expect(page).toHaveURL(/\/(?:change-password|dashboard)(?:$|\/)/);
  if (new URL(page.url()).pathname === "/change-password") {
    const newPassword = `E2eAdmin-${Date.now()}Aa1!`;
    await page.getByLabel("Contraseña actual").fill(password);
    await page.getByLabel("Nueva contraseña", { exact: true }).fill(newPassword);
    await page.getByLabel("Confirma la nueva contraseña", { exact: true }).fill(newPassword);
    await page.getByRole("button", { name: "Cambiar contraseña" }).click();
    await expect(page).toHaveURL(/\/login\?reason=password-changed/);
    await page.getByLabel("Correo electrónico").fill(email);
    await page.getByLabel("Contraseña").fill(newPassword);
    await page.getByRole("button", { name: "Iniciar sesión" }).click();
  }
  await expect(page).toHaveURL(/\/dashboard(?:$|\/)/);
}

async function createUser(page: Page, email: string, roleName: string) {
  await page.getByRole("button", { name: "Crear usuario" }).first().click();
  const dialog = page.getByRole("dialog", { name: "Crear usuario" });
  await dialog.getByLabel("Nombre completo").fill(`Prueba ${roleName}`);
  await dialog.getByLabel("Correo electrónico").fill(email);
  await dialog.getByLabel(roleName, { exact: true }).check();
  await dialog.getByRole("button", { name: "Crear usuario" }).click();
  const passwordDialog = page.getByRole("dialog", { name: "Contraseña temporal" });
  const temporaryPassword = await passwordDialog.locator("code").innerText();
  await passwordDialog.getByRole("button", { name: "Listo" }).click();
  return { email, temporaryPassword };
}

async function filterForEmail(page: Page, email: string) {
  await page.getByLabel("Buscar usuarios").fill(email);
  await expect(page).toHaveURL(new RegExp(`search=${encodeURIComponent(email)}`));
}

async function signInWithPassword(page: Page, email: string, password: string) {
  await page.goto("/login");
  await page.getByLabel("Correo electrónico").fill(email);
  await page.getByLabel("Contraseña").fill(password);
  await page.getByRole("button", { name: "Iniciar sesión" }).click();
  await expect(page).toHaveURL(/\/dashboard(?:$|\/)/);
}

async function signInAsTemporaryUser(
  browser: Browser,
  baseURL: string,
  email: string,
  temporaryPassword: string,
) {
  const context = await browser.newContext({ baseURL });
  const page = await context.newPage();
  await page.goto("/login");
  await page.getByLabel("Correo electrónico").fill(email);
  await page.getByLabel("Contraseña").fill(temporaryPassword);
  await page.getByRole("button", { name: "Iniciar sesión" }).click();
  await expect(page).toHaveURL(/\/change-password$/);
  const password = `E2eRole-${Date.now()}Aa1!`;
  await page.getByLabel("Contraseña actual").fill(temporaryPassword);
  await page.getByLabel("Nueva contraseña", { exact: true }).fill(password);
  await page.getByLabel("Confirma la nueva contraseña", { exact: true }).fill(password);
  await page.getByRole("button", { name: "Cambiar contraseña" }).click();
  await expect(page).toHaveURL(/\/login\?reason=password-changed/);
  await signInWithPassword(page, email, password);
  return { context, page, password };
}

async function getTokens(email: string, password: string): Promise<Tokens> {
  const response = await fetch(`${apiBaseUrl}/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password }),
  });
  if (!response.ok) throw new Error(`La API rechazó el inicio de sesión de prueba (${response.status}).`);
  const body: unknown = await response.json();
  if (typeof body !== "object" || body === null || !("accessToken" in body) || !("refreshToken" in body)) {
    throw new Error("La respuesta de login de prueba no contiene tokens.");
  }
  return { accessToken: String(body.accessToken), refreshToken: String(body.refreshToken) };
}

async function clearMandatoryPasswordChange(email: string, temporaryPassword: string) {
  const firstTokens = await getTokens(email, temporaryPassword);
  const newPassword = `E2e-${Date.now()}-NewAa1!`;
  const response = await fetch(`${apiBaseUrl}/auth/change-password`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${firstTokens.accessToken}`,
    },
    body: JSON.stringify({
      currentPassword: temporaryPassword,
      newPassword,
      refreshToken: firstTokens.refreshToken,
    }),
  });
  if (!response.ok) throw new Error(`No se pudo completar el cambio de contraseña de prueba (${response.status}).`);
  return getTokens(email, newPassword);
}

async function assertSystemModulesDenied(
  page: Page,
  context: BrowserContext,
  baseURL: string,
  email: string,
  temporaryPassword: string,
) {
  const tokens = await clearMandatoryPasswordChange(email, temporaryPassword);
  await context.clearCookies();
  await context.addCookies([
    { name: "gelyx-labs_access", value: tokens.accessToken, url: baseURL, httpOnly: true, sameSite: "Lax" },
    { name: "gelyx-labs_refresh", value: tokens.refreshToken, url: baseURL, httpOnly: true, sameSite: "Lax" },
  ]);
  await page.goto("/dashboard");
  await expect(page.getByRole("link", { name: "Perfil", exact: true })).toBeVisible();
  for (const linkName of ["Usuarios", "Roles", "Permisos", "Auditoría"]) {
    await expect(page.getByRole("link", { name: linkName, exact: true })).toHaveCount(0);
  }
  for (const route of ["users", "roles", "permissions", "audit"]) {
    await page.goto(`/dashboard/${route}`);
    await expect(page).toHaveURL(/\/403$/);
  }
  await context.clearCookies();
}
