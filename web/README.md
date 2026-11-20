# GelyxLabs Web

Frontend de GelyxLabs, starter web complementario a `gelyx-labs-api`.

## Stack y arquitectura

- Next.js App Router, TypeScript estricto, Tailwind CSS y componentes shadcn/ui.
- Estructura feature-based en `src/features/<feature>`.
- Server Components por defecto; React Hook Form y Zod para formularios; TanStack Query
  para datos de cliente.
- Patron BFF: el navegador llama solo a route handlers de Next.js. La API .NET se consume
  desde el servidor y los tokens se guardan en cookies `httpOnly`, `SameSite=Lax` y
  `Secure` en produccion.
- Los modulos y permisos de `/auth/me` construyen el menu y protegen las rutas; la API sigue
  siendo la autoridad de autorizacion.
- Despliegue en Vercel.

## Estructura

```text
src/
  app/                       rutas, layouts y route handlers BFF
  components/                layout y componentes compartidos
  features/<feature>/        components, hooks, services, schemas y types
  lib/                       clientes API, sesion, iconos y utilidades
tests/
  e2e/                       flujos integrados Playwright
  *.test.ts(x)               pruebas unitarias Vitest
```

## Desarrollo local

Requisitos: Node.js 20.9 o superior, pnpm 12 y la API disponible. Inicia primero
`gelyx-labs-api` con su Docker Compose y, desde la raiz de este repositorio, ejecuta:

```bash
pnpm install
cp .env.example .env.local
pnpm dev
```

Abre `http://localhost:3000`. `API_BASE_URL` debe apuntar al prefijo versionado de la API,
por defecto `http://localhost:5000/api/v1`. Esta variable es solo del servidor; el
navegador no llama directamente a la API .NET.

## Variables de entorno

| Variable | Uso |
| --- | --- |
| `API_BASE_URL` | URL de la API con prefijo versionado; solo servidor. |
| `API_OPENAPI_URL` | Documento OpenAPI que consume `pnpm api:types`. |
| `NEXT_PUBLIC_APP_NAME` | Nombre visible de la aplicacion. |
| `E2E_ADMIN_EMAIL` | Correo de Admin temporal para la prueba Playwright integrada. |
| `E2E_ADMIN_PASSWORD` | Contraseña temporal del Admin para esa prueba; no la guardes en Git. |

Los dos valores `E2E_*` solo se configuran en el entorno de prueba. `.env.example` documenta
las variables web normales; nunca incluyas contraseñas ni tokens en archivos versionados.

## Comandos utiles

```bash
pnpm dev
pnpm lint
pnpm typecheck
pnpm test
pnpm test:e2e
pnpm build
pnpm api:types
```

Para `pnpm api:types`, ejecuta la API en Development y configura `API_OPENAPI_URL` si el
documento OpenAPI no esta disponible en `http://localhost:5000/openapi/v1.json`.
El resultado se genera en `src/lib/api/generated.ts`.

## Sesion y autorizacion

La sesion se obtiene desde `/auth/me`; el menu y las rutas se derivan de sus modulos y
permisos. `requireModule` protege paginas y `<Can>` controla acciones visibles. El proxy
solo realiza una comprobacion temprana de cookies: la API sigue siendo la autoridad de
autenticacion y autorizacion. El refresco se realiza en el BFF; los eventos de foco
revalidan datos y el logout sincroniza pestañas.

## Despliegue en Vercel

1. Importa `gelyx-labs-web` desde GitHub en Vercel. Vercel detecta Next.js automaticamente;
   conserva el directorio raiz y los comandos predeterminados (`pnpm install` y `pnpm
   build`). No hace falta `vercel.json`.
2. Configura estas variables tanto en **Production** como en **Preview**:

   | Variable | Valor |
   | --- | --- |
   | `API_BASE_URL` | URL publica de Render con el prefijo de API, por ejemplo `https://<servicio>.onrender.com/api/v1` |
   | `NEXT_PUBLIC_APP_NAME` | Nombre que se muestra en la interfaz, por ejemplo `GelyxLabs` |

3. En Render, agrega el dominio de produccion de Vercel a `Cors__AllowedOrigins`. Agrega
   tambien los dominios de Preview que deban ser permitidos por CORS; cada origen debe
   coincidir con el dominio usado por el navegador.
4. Vercel establece `NODE_ENV=production` durante el despliegue de produccion. La
   aplicacion activa `Secure` en las cookies de autenticacion cuando
   `NODE_ENV=production`; confirma ese atributo en las herramientas del navegador despues
   del primer inicio de sesion.

El workflow `.github/workflows/ci.yml` ejecuta lint, typecheck, pruebas y build en cada PR
a `develop` o `main`.

## Convenciones

- Lee `AGENTS.md` antes de contribuir; codigo y comentarios en ingles, textos de UI en
  espanol.
- Usa Conventional Commits, un commit por paso logico, y ramas `feat/*` basadas en
  `develop`; integra cambios mediante PR.
- No hagas commit de secretos. No hagas push directo a `main`; actualizar `main` requiere
  un PR desde `develop` y aprobacion explicita.

## Documentacion

- `docs/adr/0001-architecture.md`: decisiones de arquitectura.
- `docs/HOW-TO-ADD-A-MODULE.md`: guia de modulos en API y web.
- `docs/NEW-PROJECT.md`: checklist para reutilizar el starter.
