# Arquitectura

Estado de la plantilla: lista verificada contra el codigo actual de `api/` y `web/`.
Cada punto marca `PRESENTE` o `AUSENTE`; ninguno se da por hecho sin revisar el codigo.

## API (.NET, Minimal APIs)

| Capacidad | Estado |
| --- | --- |
| Clean Architecture feature-based (Domain/Application/Infrastructure/Api con dependencias hacia adentro) | PRESENTE |
| CQRS propio sin MediatR (`ICommandHandler`/`IQueryHandler` + `IDispatcher`, registro por Scrutor) | PRESENTE |
| Result pattern + ProblemDetails con campo `code` | PRESENTE |
| Identity Core + JWT Bearer | PRESENTE |
| Refresh token con rotacion atomica y almacenado solo con hash SHA-256 | PRESENTE |
| Rate limiting (policies `auth` y `refresh`) | PRESENTE |
| `UseForwardedHeaders` | PRESENTE (con defaults; sin `ForwardedHeadersOptions` propio) |
| Autorizacion por permisos (`.RequirePermission("<modulo>.<accion>")`) | PRESENTE |
| Modulos (`IModuleDefinition`) y matriz de permisos por rol | PRESENTE |
| Roles dinamicos con roles de sistema protegidos (`IsSystem`) | PRESENTE |
| CRUD de usuarios (+ export CSV, cambio de estado, roles, reset de contrasena, restore) | PRESENTE |
| Auditoria (`IAuditLogger`, redaccion de claves password/token/hash/secret) | PRESENTE |
| Administracion ligera de modulos (leer, editar traducciones/icono/orden, activar/desactivar) | PRESENTE (sin endpoint de alta; `Key` inmutable) |
| Sesiones activas (listar, revocar una, revocar otras) | PRESENTE |
| Configuracion y feature flags (`app.name`, `auth.allowSelfRegistration`, `app.defaultLocale`, `flags.*`) | PRESENTE |
| Correo (`Logging`/`Resend`/`Smtp`, cola en proceso con reintentos) | PRESENTE |
| Exportacion CSV (usuarios y auditoria, con neutralizacion de formulas) | PRESENTE |
| Correlacion de errores (`X-Correlation-Id`, `Extensions["correlationId"]`) | PRESENTE |
| 2FA TOTP (setup/enable/disable, codigos de recuperacion, challenge en login) | PRESENTE |
| Locale por usuario (campo `AppUser.Locale` + `PUT auth/me/preferences`) | PRESENTE |
| Dockerfile | PRESENTE |
| docker-compose.yml | PRESENTE |
| render.yaml | PRESENTE |
| CI (PRs a develop/main) | PRESENTE |

## Web (Next.js App Router)

| Capacidad | Estado |
| --- | --- |
| App Router + Server Components por defecto | PRESENTE |
| BFF: route handlers propios, tokens en cookies `httpOnly` | PRESENTE |
| Login con `PasswordInput` y "Recordarme" (solo persiste el email) | PRESENTE |
| Sesion con revalidacion (`auth/refresh`) y logout sincronizado entre pestanas (`BroadcastChannel`) | PRESENTE |
| Sidebar dinamico construido desde los modulos de `/auth/me` | PRESENTE |
| Paginas 403, 404 y errores (`error.tsx`, `global-error.tsx`) | PRESENTE |
| i18n es/en con next-intl (locale `NEXT_LOCALE`, sin prefijo de URL) | PRESENTE |
| Notificaciones via `notify` (sonner) con traducion de codigos `errors.<code>` | PRESENTE |
| Loader global bloqueante (overlay `#app-content` con `inert`, integrado en `browser-client.ts`) | PRESENTE |
| Escala de z-index centralizada con variables CSS en `globals.css` | PRESENTE |
| Features de usuarios, roles, permisos (matriz), auditoria, modulos y ajustes | PRESENTE |
| Perfil con preferencias (locale/theme) y sesiones | PRESENTE |
| Tests Vitest (21 archivos) | PRESENTE |
| Tests Playwright E2E (`tests/e2e`) | PRESENTE |
| CI (PRs a develop/main) | PRESENTE |
| Configuracion de Vercel (`vercel.json`) | PRESENTE |

## Observaciones honestas

- No existe en el codigo ningun modulo de negocio de ejemplo: `Products` aparece solo en la
  documentacion (`docs/HOW-TO-ADD-A-MODULE.md`) como ejemplo documental.
- La API de modulos no permite crear modulos nuevos desde la UI: el modulo se declara en
  codigo (`IModuleDefinition`) y se sincroniza a la base; la UI administra metadatos.
- `docker-compose.yml`, `render.yaml`, `.env.example`, los tests y el seed usan solo
  valores de ejemplo o lectura de variables de entorno; no hay secretos versionados.
- Las migraciones EF Core se conservan tal cual; consulta `NEW-PROJECT.md` sobre cuando
  regenerarlas.