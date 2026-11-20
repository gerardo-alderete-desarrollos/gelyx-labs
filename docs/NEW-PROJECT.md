# Convertir la plantilla en un proyecto nuevo

Usa esta lista antes de compartir el starter o conectarlo a servicios externos. No reutilices
tokens, contrasenas ni bases de datos del proyecto original.

## Renombrar

1. Elige un identificador PascalCase valido como namespace .NET (ej. `AcmeLedger`) y su forma
   kebab (ej. `acme-ledger`).
2. Desde la raiz de la plantilla ejecuta el script (idempotente, no toca binarios, ignora
   `.git`, `bin`, `obj`, `node_modules`). Prueba antes con `--dry-run`:

   ```bash
   bash scripts/rename.sh --dry-run AcmeLedger acme-ledger   # revisa el listado
   bash scripts/rename.sh AcmeLedger acme-ledger
   # alternativa en maquinas sin bash:
   node scripts/rename.mjs AcmeLedger acme-ledger
   ```

   El script actualiza contenidos, nombres de archivos y carpetas, `.slnx`, proyectos `.csproj`,
   namespaces, `package.json`, `render.yaml`, `docker-compose.yml`, workflows y `docs/`.
3. Revisa el diff y resuelve cualquier nombre de producto externo o identificador que deba
   conservarse (cookies, eventos, nombres de servicios ya publicados).
4. Actualiza `api/README.md` y `web/README.md` con el nombre del proyecto nuevo.

## Secretos y configuracion

1. Genera un secreto JWT nuevo, unico por entorno:

   ```bash
   openssl rand -base64 48
   ```

2. Define `Jwt__Issuer` y `Jwt__Audience` propios del proyecto. No copies los valores de
   ejemplo.
3. Crea la base de datos nueva en Neon (proyecto nuevo, no reutilices el de la plantilla) y
   guarda la connection string como `ConnectionStrings__Default` (use el endpoint pooled para
   la app y el directo para migraciones).
4. Crea el servicio en Render con el `Dockerfile` y `render.yaml` de `api/`, y el proyecto en
   Vercel desde `web/`. Configura `NEXT_PUBLIC_APP_NAME`, `API_BASE_URL` (apunta al `render.yaml`
   desplegado, ej. `https://<proyecto>.onrender.com/api/v1`) y `API_OPENAPI_URL`.
5. Actualiza `Cors__AllowedOrigins` con el dominio real de la web (produccion y preview).
6. Define el admin inicial del seed de forma explicita en cada entorno:
   - `SEED_ADMIN_EMAIL` (p. ej. `admin@example.com`)
   - `SEED_ADMIN_PASSWORD` (secreto generado con `openssl`)
   Nuevo proyecto no tiene valores por defecto: sin estas variables el seed omite el admin.
7. Configura el proveedor de correo: `Email__Provider` = `Logging` (dev), `Resend` o `Smtp`,
   con `Email__ApiKey`, `Email__From` y, en Smtp, `Email__Smtp*`.
8. Revisa `.env.example` de `api/` y `web/`: conserva solo placeholders sin secretos. Crea los
   `.env` locales (ignorados por Git).
9. En Vercel: no definas variables de build vacias (`vercel.json` ya no las lleva); define las
   de entorno del proyecto. Elimina cualquier variable de entorno heredada de la plantilla.

## Base de datos y migraciones

- Las migraciones de la plantilla se conservan tal cual.
- Regenera la migracion inicial SOLO si cambia el esquema de la plantilla (y borra antes la
  base desechable con la que pruebes). En un proyecto nuevo sin cambios de esquema, aplica las
  migraciones existentes tal cual.
- El primer despliegue aplica migraciones con `MIGRATE_ON_STARTUP=true`; despues de verificar,
  ponlo en false si no quieres migrar en cada arranque.

## Validacion y Git

1. Regenera `src/lib/api/generated.ts` con `pnpm api:types` (en `web/`).
2. `dotnet build/test -c Release` en `api/`; `pnpm install`, `pnpm lint`, `pnpm typecheck`,
   `pnpm test` y `pnpm build` en `web/`; Playwright E2E contra el API local.
3. Prueba login por rol, menu dinamico, matriz de permisos, roles dinamicos, refresh, logout
   y cookies `Secure` en produccion.
4. Busca secretos en el repo (nunca los versiones) y confirma que `.env*` (salvo `.env.example`),
   artefactos de build y reportes esten ignorados.
5. Crea ramas `feat/*` basadas en `develop` y envia PRs a `develop`. Nunca hagas push directo a
   `main`; cualquier actualizacion de `main` requiere un PR desde `develop` y aprobacion
   explicita del owner.

## Infraestructura

Usa los CLIs autenticados (`neonctl`, `render`, `vercel`, `gh`) para aprovisionar Neon, Render
y Vercel. Confirma antes cualquier recurso de pago. Registra IDs, URLs y regiones en
`infra/RESOURCES.md` (fuera del repo publico o en la doc de despliegue) sin documentar secretos.