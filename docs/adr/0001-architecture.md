# ADR 0001: Arquitectura de la plantilla

- **Estado:** Aceptado
- **Alcance:** `api/` y `web/`

## Contexto

La plantilla es un starter reutilizable con API .NET y frontend Next.js. Necesita mantener
limites claros entre dominio, persistencia y presentacion, permitir modulos de negocio
extensibles y aplicar autorizacion dinamica por rol sin exponer credenciales al navegador.

## Decisiones

### API: Clean Architecture y organizacion por feature

La API se divide en Domain, Application, Infrastructure y Api; las dependencias apuntan hacia
adentro y Domain no depende de otras capas. Las features se organizan por funcionalidad.
Application usa handlers CQRS propios, FluentValidation y Result pattern, sin MediatR.
Infrastructure contiene EF Core, Identity, PostgreSQL y servicios externos. La capa Api expone
Minimal APIs y convierte resultados de negocio en respuestas HTTP/ProblemDetails con `code`.

### Web: BFF y cookies

El navegador nunca llama directamente a la API .NET. Los route handlers de Next.js actuan
como BFF y guardan los tokens exclusivamente en cookies `httpOnly`, `SameSite=Lax` y `Secure`
en produccion. Los tokens no se guardan en `localStorage`.

### Refresh rotativo y sesiones

Los refresh tokens se rotan al renovar la sesion y se revocan en las operaciones de seguridad
pertinentes; solo se persiste el hash. La web revalida datos al recuperar el foco; el logout y
el cambio de contrasena se sincronizan entre pestanas mediante mensajeria del navegador.
Desactivar una cuenta invalida su siguiente refresh.

### Modulos y permisos por rol

Cada feature de la API declara un `IModuleDefinition`; los permisos siguen `<modulo>.<accion>`
y los endpoints usan `.RequirePermission(...)`. En la web, `requireModule` protege rutas y
`<Can>` controla acciones. La matriz editable asigna permisos a roles y determina la
visibilidad del menu.

Los permisos se resuelven en el servidor y se exponen por `/auth/me`; no se incluyen en el JWT.
La UI es una ayuda de navegacion, no un limite de seguridad: la API es la autoridad. Los
modulos de sistema (`users`, `roles`, `permissions` y `audit`) son exclusivos de Admin. La fila
de Admin en la matriz es inmutable y siempre conserva todos los permisos.

### Roles base protegidos y roles dinamicos

Admin, Soporte y Usuario son roles de sistema: no se renombran ni eliminan. Admin puede crear
roles adicionales. Las listas se consultan desde la API y no se codifican como catalogos fijos
en API o web.

### Infraestructura CLI-first

Las operaciones de GitHub, Neon, Render y Vercel se realizan primero con sus CLIs autenticados.
Se usa una API REST solo si el CLI no soporta la operacion. Los secretos se gestionan en
variables de entorno y nunca se versionan; los recursos creados se documentan sin secretos en
`infra/RESOURCES.md`.

## Consecuencias

- Las features nuevas registran su propio modulo y sus permisos, en vez de ampliar listas
  centrales de roles o modulos.
- La API aplica cada permiso aunque la web oculte el control correspondiente.
- Los cambios de esquema se entregan como migraciones EF Core revisadas.
- Los despliegues y el manejo de secretos siguen los procedimientos documentados del starter.