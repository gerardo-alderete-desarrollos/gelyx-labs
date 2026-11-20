# Convenciones

Reglas que aplican a todo el codigo de `api/` y `web/`.

## Codigo y textos

- Codigo y comentarios en ingles; textos de UI en espanol (e ingles via i18n).
- Sin secretos en el repo: variables de entorno y `.env.example` actualizado.
- Conventional Commits; un commit por paso logico.
- Antes de terminar cada tarea: build, lint y tests, y reportar el resultado.
- Todo cambio lleva tests.

## Ramas

- `develop` es la rama de trabajo e integracion.
- El trabajo nuevo va en ramas `feat/*` que se integran a `develop` mediante PR.
- Nunca se hace commit ni push directo a `main`; `main` solo se actualiza con un PR desde
  `develop` y con aprobacion explicita del owner.
- Si una decision no esta definida, pregunta antes de asumir.

## API

- Organizacion feature-based; CQRS propio sin MediatR.
- Result pattern para errores de negocio; ProblemDetails (RFC 7807) para errores HTTP; la API
  devuelve `code` (no traduce: la web traduce `errors.<code>`).
- Roles base Admin/Soporte/Usuario protegidos (`IsSystem=true`) y roles dinamicos creados desde
  la UI. Nunca asumir una lista fija de roles en codigo.
- Toda funcionalidad pertenece a un modulo (`IModuleDefinition`) con permisos
  `<modulo>.<accion>` (access/read/create/update/delete minimos).
- Nunca proteger endpoints con roles escritos a mano: siempre `.RequirePermission(...)`.
- Toda accion sensible se audita con `IAuditLogger`.
- Los modulos de sistema (users, roles, permissions, audit) son exclusivos de Admin.

## Web

- Todo z-index usa la escala de capas definida en `src/app/globals.css`
  (`--z-sticky-col:10`, `--z-sticky-head:20`, `--z-sticky-corner:30`, `--z-app-header:40`,
  `--z-changes-bar:50`, `--z-overlay:60`, `--z-toaster:70`, `--z-dialog:80`, `--z-dropdown:90`).
  Prohibido z-index numericos sueltos; todo elemento sticky lleva fondo opaco.
- Todo campo de contrasena usa `PasswordInput`. Nunca se persisten contrasenas ni tokens en el
  navegador; solo el email puede recordarse (autocomplete del gestor).
- Toda llamada a la API desde el cliente pasa por `src/lib/api/client.ts`. Las mutaciones
  bloquean la UI con el overlay global; las lecturas en segundo plano usan `silent:true`. Todo
  formulario deshabilita su envio mientras su mutacion esta pendiente.
- Toda notificacion pasa por `src/lib/notify.ts`; nunca usar sonner directamente.
- Menu y rutas se construyen a partir de los modulos y permisos de `/auth/me`; no se agregan
  entradas fijas al sidebar.
- Patron BFF: el navegador nunca llama a la API .NET directo; tokens solo en cookies httpOnly.