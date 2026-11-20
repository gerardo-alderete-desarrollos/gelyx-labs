# AGENTS.md

Reglas para todo el proyecto. Hay dos componentes: `api/` (.NET) y `web/` (Next.js).

## Contexto

Proyecto base reutilizable ("starter"). Dos carpetas: `api/` y `web/`. Para crear un proyecto
nuevo, sigue `docs/NEW-PROJECT.md` y usa `scripts/rename.sh` (o `scripts/rename.mjs`).

## Stack

- API: .NET, ASP.NET Core Minimal APIs, EF Core + Npgsql (PostgreSQL en Neon),
  ASP.NET Core Identity, JWT.
- Web: Next.js (ultima estable, App Router), TypeScript estricto, Tailwind, shadcn/ui.
- Deploy: API en Render (Docker), BD en Neon, Web en Vercel.

## Arquitectura API

- Clean Architecture: Domain, Application, Infrastructure, Api. Las dependencias apuntan hacia
  adentro; Domain no referencia a nadie.
- Organizacion feature-based (carpeta por feature).
- CQRS ligero con handlers propios (ICommandHandler/IQueryHandler). NO usar MediatR.
- Result pattern para errores de negocio; ProblemDetails (RFC 7807) para errores HTTP.
- FluentValidation. Mapeo manual o Mapster (no AutoMapper).

## Modelo de autorizacion (regla central)

- Roles base: Admin, Soporte, Usuario. Son roles protegidos (IsSystem=true): no se pueden
  renombrar ni eliminar. El Admin puede crear roles adicionales desde la UI. Nunca asumir una
  lista fija de roles en el codigo de la API ni de la web: los roles se cargan siempre desde
  la API.
- "Admin" es el unico rol con acceso total y a modulos de sistema. El rol por defecto del
  auto-registro es "Usuario".
- Toda funcionalidad pertenece a un MODULO. Cada modulo declara sus permisos con la convencion
  "<modulo>.<accion>".
- Acciones estandar por modulo: access, read, create, update, delete. Acciones adicionales solo
  cuando sean necesarias y con verbo claro (export, approve, etc.). Todo modulo CRUD declara al
  menos las cinco estandar.
- "<modulo>.access" hace visible el modulo en el menu y permite entrar. Sin el, la API responde
  403 y la web no muestra el modulo.
- Los permisos se asignan a ROLES desde la base de datos (matriz editable por Admin).
- Los modulos de sistema (users, roles, permissions, audit) son EXCLUSIVOS de Admin: no se
  pueden asignar a otros roles ni por la API ni por la UI.
- La fila/columna de Admin en la matriz no es editable y siempre tiene todos los permisos.
- Cada feature nueva registra su modulo implementando IModuleDefinition. Nunca proteger endpoints
  con roles escritos a mano: usar siempre policies por permiso (.RequirePermission("modulo.accion")).
- Los permisos NO van en el JWT: se resuelven en el servidor y se exponen via el endpoint de
  sesion (/auth/me).
- La UI oculta lo que no corresponde, pero la API es siempre la fuente de verdad.

## Arquitectura Web

- Estructura feature-based: src/features/<feature>/{components,hooks,services,schemas,types}.
- Server Components por defecto; "use client" solo cuando sea necesario.
- react-hook-form + zod para formularios; TanStack Query para datos de cliente.
- Patron BFF: el navegador nunca llama a la API .NET directo. Tokens SOLO en cookies httpOnly,
  nunca en localStorage.
- Menu y rutas se construyen a partir de los modulos y permisos que devuelve el endpoint de
  sesion. Agregar un modulo nuevo no debe requerir tocar el sidebar.

## Operacion con CLIs (CLI-first)

- Para toda operacion de infraestructura usa primero los CLIs: gh (GitHub), neonctl (Neon),
  render (Render) y vercel (Vercel). Solo si un CLI no soporta la accion, usa su API REST; y
  solo si tampoco es posible, da instrucciones manuales.
- Antes de usar un CLI verifica su autenticacion (gh auth status, neonctl me, render whoami,
  vercel whoami). Si no hay sesion, lanza el comando de login (se abrira un modal o navegador
  para que el usuario autorice) y espera a que termine.
- Ante la duda sobre flags, consulta --help antes de ejecutar; no inventes comandos.
- Secretos: nunca los imprimas en la salida, no los commitees ni los pongas en archivos
  versionados. Generalo con openssl, guardalos en .env.local (ignorado por git) y cargalos a
  cada servicio por stdin o variable de entorno.
- Pide confirmacion antes de acciones que creen recursos de pago, borren recursos o hagan push
  a main. Muestra un resumen de lo que se va a crear antes de crearlo.
- Registra cada recurso creado (IDs, URLs, regiones) en infra/RESOURCES.md, sin secretos.

## Reglas generales

- Codigo y comentarios en ingles, textos de UI en espanol.
- Sin secretos en el repo: variables de entorno y .env.example actualizado.
- Conventional Commits, un commit por paso logico.
- Antes de terminar cada tarea: build, lint y tests, y reportar el resultado.
- Los resultados de build, lint, tests y comandos relevantes se guardan en reports/ (carpeta
  ignorada por git) y en consola se muestra solo un resumen breve. Nunca escribas secretos en
  los reportes (connection strings, tokens, contrasenas, claves); enmascaralos como ***.
- Ramas: `develop` es la rama de trabajo e integracion. El trabajo nuevo va en ramas `feat/*`
  que se integran a `develop` mediante PR. Nunca hagas commit ni push directo a `main`; `main`
  solo se actualiza con un PR desde `develop` y con aprobacion explicita.
- Si una decision no esta definida, pregunta antes de asumir.
- Todo texto de UI va en los catalogos de idioma (messages/es.json y messages/en.json); nunca
  hardcodeado.
- La API no traduce: devuelve `code`; la web traduce `errors.<code>`. Todo codigo de error
  nuevo requiere traduccion en ambos idiomas.
- Toda notificacion pasa por el modulo de notificacion de la web; nunca usar sonner
  directamente.
- Todo modulo nuevo se crea en codigo implementando IModuleDefinition y siguiendo
  docs/HOW-TO-ADD-A-MODULE.md, con traducciones de nombre en espanol e ingles. La UI solo
  administra modulos existentes.
- Toda accion sensible se audita con IAuditLogger.
- Todo cambio lleva tests; los reportes no son evidencia, la salida de comandos si.
- Todo campo de contrasena usa PasswordInput. Nunca se persisten contrasenas ni tokens en el
  navegador; solo el email puede recordarse, y la contrasena la gestiona el gestor del
  navegador mediante autocomplete.
- Todo z-index usa la escala de capas definida en la web (globals.css); prohibido z-index
  numericos sueltos. Todo elemento sticky lleva fondo opaco.
- Toda llamada a la API desde el cliente pasa por el cliente unico de la web. Las mutaciones
  bloquean la UI con el overlay global; las lecturas en segundo plano usan modo silencioso.
  Todo formulario deshabilita su envio mientras su mutacion esta pendiente.