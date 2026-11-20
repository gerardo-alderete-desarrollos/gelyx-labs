# Como agregar un modulo

Esta guia describe el flujo completo para una feature de ejemplo llamada `Products`. Es un
ejemplo documental: no agrega ni conserva el modulo `products` en el codigo de la plantilla.

Regla base: nunca compruebes nombres de roles escritos a mano. Todo modulo se declara en
codigo implementando `IModuleDefinition` y sus endpoints se protegen con
`.RequirePermission("<modulo>.<accion>")`.

## 1. API

1. Crea `src/<App>.Application/Features/Products/` y organiza ahi los comandos, consultas,
   DTOs, handlers y validadores. Usa los contratos CQRS (`ICommandHandler`/`IQueryHandler`),
   FluentValidation y Result pattern del proyecto; no agregues MediatR.
2. Implementa `IModuleDefinition` en `ProductsModule`:
   ```csharp
   public class ProductsModule : IModuleDefinition
   {
       public string Key => "products";
       public string Name => "Productos";
       public string? Description => "Gestion de productos";
       public IReadOnlyDictionary<string, ModuleTranslation> Translations => new Dictionary<string, ModuleTranslation>
       {
           ["es"] = new() { Name = "Productos", Description = "Gestion de productos" },
           ["en"] = new() { Name = "Products", Description = "Products management" }
       };
       public string? Icon => "package";
       public string? Route => "/dashboard/products";
       public int SortOrder => 60;
       public bool IsSystem => false;
       public IEnumerable<(string Permission, string Description)> GetPermissions()
       {
           yield return ("products.access", "Acceso al modulo de productos");
           yield return ("products.read", "Leer productos");
           yield return ("products.create", "Crear productos");
           yield return ("products.update", "Actualizar productos");
           yield return ("products.delete", "Eliminar productos");
       }
   }
   ```
3. Declara al menos las cinco acciones estandar de un modulo CRUD:
   `products.access`, `products.read`, `products.create`, `products.update`, `products.delete`.
   `<modulo>.access` habilita la entrada al modulo y su visibilidad en el menu.
4. Crea los endpoints en `src/<App>.Api/Endpoints/` y protege cada operacion con el permiso
   correspondiente (ej. `.RequirePermission("products.read")`).
5. No registres el modulo a mano: la aplicacion descubre las definiciones `IModuleDefinition`
   y el `ModuleSynchronizer` sincroniza modulo y permisos a la base y los asigna a Admin.
6. Si la feature cambia persistencia, crea una migracion EF Core desde la API, revisa el SQL
   y el modelo generado, y prueba la migracion sobre una base desechable.
7. Audita toda accion sensible con `IAuditLogger` (ej. `products.created`).
8. Las traducciones de nombre viven en el propio `IModuleDefinition` (es/en).

Los modulos de sistema (`users`, `roles`, `permissions`, `audit`) son exclusivos de Admin:
no se marcan como negocio y no se asignan a otros roles.

### Exportar una tabla a CSV

1. Agrega `GET <recurso>/export` junto al listado y protegelo con `.RequirePermission("<modulo>.read")`.
2. Reutiliza la consulta y los filtros del listado; el repositorio expone un `IAsyncEnumerable`
   ordenado, aplica `Take(10_000)` y no materializa el conjunto completo.
3. Escribe con `Results.Stream`, `text/csv; charset=utf-8` y nombre de descarga; escapa comillas,
   comas y saltos de linea y antepone `'` a `=`, `+`, `-` o `@` iniciales.
4. Registra `export.created` con `IAuditLogger` (recurso, filtros y limite) y agrega pruebas.
5. En la web usa el boton `ExportCsvButton` con los filtros actuales, sus traducciones y el permiso.

## 2. Web

1. Crea `src/features/products/` con `components`, `hooks`, `services`, `schemas` y `types`.
   Las llamadas pasan por el BFF; el navegador nunca usa la URL .NET directa.
2. Crea `src/app/(protected)/dashboard/products/page.tsx` como Server Component y llama a
   `requireModule("products")` antes de renderizar; los controles de accion usan
   `<Can permission="products.create">` (o el permiso que corresponda).
3. Agrega a `src/lib/icon-map.ts` la asociacion del identificador de icono enviado por la API
   (en el ejemplo, `package`) a un icono Lucide.
4. No agregues una entrada fija al sidebar: menu, ruta y acceso se derivan de los modulos y
   permisos de `/auth/me`.
5. Agrega los textos de UI a `messages/es.json` y `messages/en.json` (nunca hardcodeados) y los
   codigos de error nuevos a ambos idiomas bajo `errors.<code>`.
6. Las mutaciones llaman a la API desde `src/lib/api/client.ts` (`fetchBff`/BFF routes) y las
   notificaciones pasan por `src/lib/notify.ts`.
7. Si cambia el contrato OpenAPI, regenera los tipos con `pnpm api:types`.
8. Agrega pruebas Vitest de componentes/servicios y Playwright E2E del flujo.

## 3. Asignar el modulo y probarlo

1. Inicia sesion como Admin y abre `/dashboard/permissions`: revisa la matriz y asigna a cada
   rol solo los permisos de `products` que necesita; `products.access` habilita la entrada.
2. Para un rol nuevo: `/dashboard/roles` > **Crear rol** > nombre > guardar; luego **Ver
   permisos** en ese rol y asigna los modulos desde la matriz.
3. Revisa `/dashboard/modules` para ver el modulo sincronizado (icono, orden, traducciones).
4. Comprueba el menu y las rutas con un usuario de cada rol. Sin permiso, la web oculta el
   modulo y la API responde 403 en la operacion protegida.

## Lista de verificacion

- [ ] `IModuleDefinition` con `Key`, `Name`, `Translations` es/en, `Icon`, `Route`, `SortOrder`, `IsSystem`
- [ ] Permisos `<modulo>.access/read/create/update/delete` declarados en codigo
- [ ] Endpoints con `.RequirePermission(...)`, sin comprobar roles a mano
- [ ] Auditoria con `IAuditLogger` en acciones sensibles
- [ ] Migracion EF Core si hay cambios de esquema
- [ ] Pagina web con `requireModule("products")` y `Can`
- [ ] Icono registrado en `icon-map.ts`
- [ ] Textos en `messages/es.json` y `messages/en.json` (y codigos de error en ambos)
- [ ] Llamadas BFF desde `client.ts`; notificaciones via `notify`
- [ ] Asignacion probada desde `/dashboard/permissions` y administracion desde `/dashboard/modules`
- [ ] Tests pasando (build, lint, typecheck, tests, E2E)