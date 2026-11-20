# AGENTS.md — GelyxLabs API

Reglas para este repositorio (`gelyx-labs-api`). El proyecto GelyxLabs tiene dos repos:
`gelyx-labs-api` (este) y `gelyx-labs-web`, que tiene su propio `AGENTS.md`.

## Contexto

Proyecto base reutilizable ("starter") llamado GelyxLabs. Este repositorio contiene el
backend: .NET 10, ASP.NET Core Minimal APIs, EF Core + Npgsql (PostgreSQL en Neon),
ASP.NET Core Identity, JWT. Deploy en Render (Docker), BD en Neon.

## Stack

- .NET 10, ASP.NET Core Minimal APIs, EF Core + Npgsql (PostgreSQL en Neon),
  ASP.NET Core Identity, JWT.
- Deploy: API en Render (Docker), BD en Neon.

## Arquitectura API

- Clean Architecture: Domain, Application, Infrastructure, Api. Las dependencias apuntan
  hacia adentro; Domain no referencia a nadie.
- Organización feature-based (carpeta por feature: Auth, Users, Roles, Permissions, etc.).
- CQRS ligero con handlers propios (ICommandHandler/IQueryHandler). NO usar MediatR.
- Result pattern para errores de negocio; ProblemDetails (RFC 7807) para errores HTTP.
- FluentValidation. Mapeo manual o Mapster (no AutoMapper).

## Modelo de autorización (regla central)

- Roles base: Admin, Soporte, Usuario. Son roles protegidos (IsSystem=true): no se pueden
  renombrar ni eliminar. El Admin puede crear roles adicionales desde la UI. Nunca asumir
  una lista fija de roles en el código de la API ni de la web: los roles se cargan siempre
  desde GET /api/v1/roles.
- "Admin" es el único rol con acceso total y a módulos de sistema. El rol por defecto del
  auto-registro es "Usuario".
- Toda funcionalidad pertenece a un MÓDULO (ej. users, roles, permissions, audit,
  products). Cada módulo declara sus permisos con la convención "<modulo>.<accion>".
- Acciones estándar por módulo: access, read, create, update, delete. Acciones adicionales
  solo cuando sean necesarias y con verbo claro (export, approve, etc.). Todo módulo CRUD
  declara al menos las cinco estándar.
- "<modulo>.access" hace visible el módulo en el menú y permite entrar. Sin él, la API
  responde 403 y la web no muestra el módulo.
- Los permisos se asignan a ROLES desde la base de datos (matriz editable por Admin).
- Los módulos de sistema (users, roles, permissions, audit) son EXCLUSIVOS de Admin: no se
  pueden asignar a otros roles ni por la API ni por la UI.
- La fila/columna de Admin en la matriz no es editable y siempre tiene todos los permisos.
- Cada feature nueva registra su módulo implementando IModuleDefinition. Nunca proteger
  endpoints con roles escritos a mano: usar siempre policies por permiso
  (.RequirePermission("modulo.accion")).
- Los permisos NO van en el JWT: se resuelven en el servidor y se exponen vía /auth/me.
- La UI oculta lo que no corresponde, pero la API es siempre la fuente de verdad.

## Operación con CLIs (CLI-first)

- Para toda operación de infraestructura usa primero los CLIs: gh (GitHub), neonctl (Neon),
  render (Render) y vercel (Vercel). Solo si un CLI no soporta la acción, usa su API REST;
  y solo si tampoco es posible, dame instrucciones manuales.
- Antes de usar un CLI verifica su autenticación (gh auth status, neonctl me, render
  whoami, vercel whoami). Si no hay sesión, lanza el comando de login (se abrirá un modal
  o navegador para que yo autorice) y espera a que termine.
- Ante la duda sobre flags, consulta --help antes de ejecutar; no inventes comandos.
- Secretos: nunca los imprimas en la salida, no los commitees ni los pongas en archivos
  versionados. Genéralos con openssl, guárdalos en .env.local (ignorado por git) y
  cárgalos a cada servicio por stdin o variable de entorno.
- Pídeme confirmación antes de acciones que creen recursos de pago, borren recursos o
  hagan push a main. Muéstrame un resumen de lo que vas a crear antes de crearlo.
- Registra cada recurso creado (IDs, URLs, regiones) en infra/RESOURCES.md, sin secretos.

## Reglas generales

- Código y comentarios en inglés, textos de UI en español.
- Sin secretos en el repo: variables de entorno y .env.example actualizado.
- Conventional Commits, un commit por paso lógico.
- Antes de terminar cada tarea: build, lint y tests, y reportar el resultado.
- Los resultados de build, lint, tests y comandos relevantes se guardan en
  reports/<tarea>.txt (carpeta ignorada por git) y en consola se muestra solo un resumen
  breve. Nunca escribas secretos en los reportes (connection strings, tokens, contraseñas,
  claves); enmascáralos como ***.
- Ramas: `develop` es la rama de trabajo e integración. El trabajo nuevo va en ramas
  `feat/*` que se integran a `develop` mediante PR. Nunca hagas commit ni push directo a
  `main`; `main` solo se actualiza con un PR desde `develop` y con mi OK explícito.
- Si una decisión no está definida, pregunta antes de asumir.
- Todo texto de UI va en `messages/es.json` y `messages/en.json`; nunca hardcodeado.
- La API no traduce: devuelve `code`; la web traduce `errors.<code>`. Todo código de error nuevo requiere traducción en ambos idiomas.
- Toda notificación pasa por `src/lib/notify.ts`; nunca usar sonner directamente.
- Todo módulo nuevo se crea en código implementando `IModuleDefinition` y siguiendo `HOW-TO-ADD-A-MODULE.md`, con traducciones de nombre en español e inglés. La UI solo administra módulos existentes.
- Toda acción sensible se audita con `IAuditLogger`.
- Todo cambio lleva tests; los reportes no son evidencia, la salida de comandos sí.