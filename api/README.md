# GelyxLabs API

API REST de GelyxLabs para autenticacion, usuarios, roles, permisos y auditoria. Es parte
del starter de dos repositorios; `gelyx-labs-web` contiene la aplicacion Next.js.

## Stack y arquitectura

- .NET 10, ASP.NET Core Minimal APIs, EF Core, Npgsql y PostgreSQL.
- ASP.NET Core Identity para usuarios y roles; JWT con refresh tokens rotativos.
- Clean Architecture: `Domain`, `Application`, `Infrastructure` y `Api`; las dependencias
  apuntan hacia adentro.
- Features organizadas por funcionalidad, handlers CQRS propios y Result pattern. No se
  usa MediatR.
- Autorizacion por modulo y permiso (`<modulo>.<accion>`), resuelta en el servidor.
- Produccion: imagen Docker en Render y PostgreSQL en Neon.

## Estructura

```text
src/
  GelyxLabs.Domain/          entidades y reglas de dominio
  GelyxLabs.Application/     features, contratos, validacion y modulos
  GelyxLabs.Infrastructure/  EF Core, Identity, servicios y migraciones
  GelyxLabs.Api/             Minimal APIs, middleware y configuracion
tests/
  GelyxLabs.UnitTests/
  GelyxLabs.IntegrationTests/
```

## Desarrollo local

Requisitos: .NET SDK 10 y Docker Compose. Desde la raiz del repositorio:

```bash
cp .env.example .env
# Edita .env: cambia los valores de ejemplo, especialmente secretos y credenciales locales.
docker compose up --build
```

Compose inicia PostgreSQL 17 y la API, aplica las migraciones y crea el Admin inicial.
La API queda disponible en `http://localhost:8080`; comprueba `http://localhost:8080/health`.
No reutilices las credenciales de ejemplo fuera de un entorno local aislado.

Tambien puedes ejecutar la API desde el SDK si PostgreSQL esta disponible:

```bash
dotnet restore GelyxLabs.slnx
dotnet run --project src/GelyxLabs.Api
```

## Variables de entorno

Compose toma las variables locales de `.env`; Render recibe las variables de la API con
notacion `__` de .NET. Los valores secretos no deben guardarse en Git.

| Variable | Uso |
| --- | --- |
| `POSTGRES_PASSWORD` | Contraseña local de PostgreSQL en Compose. |
| `JWT_SECRET` / `Jwt__Secret` | Clave de firma JWT; usa un valor aleatorio y largo. |
| `JWT_ISSUER` / `Jwt__Issuer` | Emisor esperado por la API y sus tokens. |
| `JWT_AUDIENCE` / `Jwt__Audience` | Audiencia esperada por la API y sus tokens. |
| `PORT` | Puerto publicado y escuchado por la API en Compose (por defecto `8080`). |
| `DB_PORT` | Puerto local publicado por PostgreSQL en Compose (por defecto `5432`). |
| `CORS_ALLOWED_ORIGINS` / `Cors__AllowedOrigins` | Origenes permitidos, separados por coma. |
| `SEED_ADMIN_EMAIL` | Correo para crear el Admin inicial. |
| `SEED_ADMIN_PASSWORD` | Contraseña local del Admin inicial. |
| `AUTH_ALLOW_SELF_REGISTRATION` / `Auth__AllowSelfRegistration` | Habilita el auto-registro. |
| `ConnectionStrings__Default` | Connection string de PostgreSQL requerida por Render. |
| `ASPNETCORE_ENVIRONMENT` | Entorno ASP.NET Core; Compose usa `Development` por defecto. |
| `MIGRATE_ON_STARTUP` | Controla la aplicacion de migraciones al iniciar el contenedor. |

Consulta `.env.example` y `render.yaml` para los nombres concretos usados por cada entorno.
En produccion usa el endpoint pooled de Neon para la aplicacion y protege las credenciales
en el gestor de variables del servicio.

## Comandos utiles

```bash
dotnet restore GelyxLabs.slnx
dotnet build GelyxLabs.slnx --configuration Release
dotnet test GelyxLabs.slnx --configuration Release
dotnet run --project src/GelyxLabs.Api
```

Para crear una migracion, instala o usa `dotnet-ef` y apunta el proyecto de migraciones a
Infrastructure y el startup project a Api. Revisa la migracion generada antes de aplicarla.

## Autorizacion y modulos

Cada feature declara un `IModuleDefinition` y sus permisos. Las rutas se protegen con
`.RequirePermission("modulo.accion")`, nunca con nombres de rol codificados en el endpoint.
Los permisos se asignan a roles desde la matriz y se resuelven en el servidor; no se incluyen
en el JWT. Admin es el unico rol con acceso total y a los modulos de sistema. Consulta
`docs/HOW-TO-ADD-A-MODULE.md` para agregar una feature.

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
