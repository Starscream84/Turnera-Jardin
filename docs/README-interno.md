> 📌 **Documento interno del equipo.** Cómo está levantado el sistema, cómo correrlo en local y cómo operarlo. Para la presentación del proyecto ver el [`README.md`](../README.md) principal. El repositorio es privado desde el 27/10/2026, pero igual acá no van contraseñas, claves ni datos personales: cualquier colaborador o servicio con acceso al repo los vería.

# Turnera Web — guía interna

Estado al 27/10/2026: todo el trabajo vive en `main`, que es la rama que despliegan Railway y Vercel.

## Índice

1. [Arquitectura](#1-arquitectura)
2. [Producción](#2-producción)
3. [Variables de entorno](#3-variables-de-entorno)
4. [Desarrollo local](#4-desarrollo-local)
5. [Base de datos](#5-base-de-datos)
6. [Cómo se despliega un cambio](#6-cómo-se-despliega-un-cambio)
7. [Emails (Brevo)](#7-emails-brevo)
8. [WhatsApp](#8-whatsapp)
9. [Reglas de negocio](#9-reglas-de-negocio)
10. [Referencia de la API](#10-referencia-de-la-api)
11. [Configuración que vive en el código](#11-configuración-que-vive-en-el-código)
12. [Seguridad](#12-seguridad)
13. [Problemas conocidos y cómo resolverlos](#13-problemas-conocidos-y-cómo-resolverlos)
14. [Pendientes](#14-pendientes)

---

## 1. Arquitectura

```
 Familias / personal (navegador)
            │
            ▼
┌───────────────────────────┐     HTTPS/JSON     ┌──────────────────────────────┐
│ Vercel                    │ ─────────────────▶ │ Railway — servicio backend   │
│ Angular 22 (sitio estático)│                    │ ASP.NET Core 8 (Docker, :8080)│
└───────────────────────────┘                    └───────┬──────────────┬───────┘
                                                         │ red interna  │ HTTPS
                                                         ▼              ▼
                                           ┌─────────────────────┐  ┌──────────┐
                                           │ Railway — MySQL 8.4 │  │ Brevo    │
                                           │ (sin acceso público) │  │ (emails) │
                                           └─────────────────────┘  └──────────┘
```

| Capa | Tecnología |
|---|---|
| Backend | ASP.NET Core 8 Web API, EF Core con Pomelo (MySQL), JWT + BCrypt, rate limiting nativo |
| Frontend | Angular 22, standalone components, signals, Reactive Forms, sin librerías de UI. Build con `@angular/build`. Estilos globales en `frontend/src/styles.css` (no hay CSS por componente) |
| Base | MySQL 8.4 en Railway; MySQL 8 local para desarrollo |
| Emails | API HTTP de Brevo (`BrevoEmailService`) |

El código (clases, métodos, rutas de la API, CSS) está en inglés. Los textos que ven las familias y el personal, y las URLs de las pantallas (`/reservar`, `/admin/turnos`…), están en español.

### Estructura de carpetas

```
backend/
├── TurneraJardin.sln
├── scripts/rename-to-english.sql   (migración puntual del 24/10, ver sección 5)
└── TurneraJardin.Api/
    ├── Controllers/    Auth, Teachers, Appointments (públicos) y Admin* (panel)
    ├── Models/         Teacher, Appointment, User + Enums/
    ├── Dtos/
    ├── Data/           AppDbContext, DbSeeder
    ├── Services/       JWT, BrevoEmailService, ArgentinaDate, WhatsApp (sin usar)
    ├── wwwroot/fotos-perfil/   fotos de perfil (volumen en producción)
    ├── Dockerfile
    └── Program.cs
frontend/
├── vercel.json
└── src/
    ├── environments/   environment.ts (local) y environment.prod.ts (producción)
    ├── styles.css
    └── app/
        ├── core/       config/, guards/, interceptors/, models/, services/
        ├── shared/     public-shell (marco de las pantallas públicas), avatar.util
        ├── public-booking/   select-teacher, select-appointment, booking-form, cancel-appointment
        └── admin/      login, layout, appointments, teachers, users, my-account
```

## 2. Producción

| Pieza | Dónde | Detalle |
|---|---|---|
| Frontend | Vercel | https://turnera-jardin-azure.vercel.app — Root Directory `frontend`, Production Branch `main`. `frontend/vercel.json` define build (`ng build`), salida (`dist/frontend/browser`) y el rewrite a `index.html` para que funcionen las rutas de Angular. |
| Backend | Railway (proyecto `laudable-charisma`, entorno `production`) | https://turnera-jardin-production.up.railway.app — rama `main`, build con el `Dockerfile` de `backend/TurneraJardin.Api`, escucha en el puerto 8080, `ASPNETCORE_ENVIRONMENT=Production` (Swagger apagado). |
| Base | Railway, servicio `MySQL` del mismo proyecto | MySQL 8.4, solo red interna (`mysql.railway.internal`). Sin acceso público. |
| Fotos de perfil | Volumen de Railway | Montado en `/app/wwwroot/fotos-perfil` para que no se pierdan entre deploys. |
| Emails | Brevo | Remitente verificado en la cuenta de Brevo. |

- Acceso del personal: https://turnera-jardin-azure.vercel.app/admin/login (no hay link desde la parte pública).
- El frontend llama a la API con la URL de `frontend/src/environments/environment.prod.ts`. Si cambia el dominio del backend, se cambia ahí.
- CORS: los orígenes permitidos están en `Cors:AllowedOrigins` de `appsettings.json` (`http://localhost:4200` y el dominio de Vercel). Si cambia el dominio del frontend, hay que agregarlo ahí o con variables `Cors__AllowedOrigins__0`, `__1`…
- Hosting: hoy es el plan de prueba de Railway. **El hosting a largo plazo está pendiente de decisión del grupo.** Antes de cualquier cambio de plan o de proveedor, hacer backup de la base (sección 5).

## 3. Variables de entorno

En .NET, `__` en el nombre de la variable equivale a `:` en `appsettings.json` (`Email__ApiKey` → `Email:ApiKey`). Las variables pisan lo que diga el archivo.

### Backend en Railway

| Variable | Obligatoria | Valor |
|---|---|---|
| `ConnectionStrings__Default` | Sí | Armada con referencias al servicio MySQL: `Server=${{MySQL.MYSQLHOST}};Port=${{MySQL.MYSQLPORT}};Database=${{MySQL.MYSQLDATABASE}};User=${{MySQL.MYSQLUSER}};Password=${{MySQL.MYSQLPASSWORD}}` |
| `Jwt__Key` | Sí | Clave aleatoria de **32 caracteres o más**. Solo existe en Railway. |
| `Email__Enabled` | Sí | `true` |
| `Email__ApiKey` | Sí | API key de Brevo |
| `Email__SenderEmail` | Sí | Email remitente verificado en Brevo |
| `Seed__AdminPassword` | No | Solo para una base **nueva**: contraseña inicial de `admin@jardin.edu.ar` (12+ caracteres). Sin ella se genera una al azar y se escribe una vez en el log |
| `ForwardedHeaders__KnownNetworks__0` | No | Rango del proxy de Railway (ej. `10.0.0.0/8`) si se quiere restringir de quién se acepta `X-Forwarded-For` |

Si falta `ConnectionStrings:Default`, o `Jwt:Key` es más corta que 32 caracteres, la API **no arranca** (lo controla `Program.cs`) y el log de Railway lo dice.

**Health check:** `GET /health` responde 200 si la API llega a MySQL. Se puede configurar en Railway (Settings → Deploy → Healthcheck Path = `/health`) para que un deploy roto no reemplace al que funciona.

Lo demás sale de `appsettings.json`: `Email:SenderName` ("Jardín IDRA"), `Email:FrontendUrl` (link de los emails), `Jwt:ExpirationHours` (8), CORS y la sección `WhatsApp` (deshabilitada).

**Nombres viejos:** antes de la traducción al inglés (24/10) las claves estaban en español (`Email__Habilitado`, `Email__RemitenteEmail`, `Jwt__HorasExpiracion`, `Cors__OrigenesPermitidos__N`, `WhatsApp__Habilitado`…). El código ya no las lee: si quedan en Railway, se pueden borrar una vez confirmado que el email llega con las nuevas.

### Frontend en Vercel

No usa variables de entorno: la URL de la API está en `environment.prod.ts`, que se compila dentro del sitio.

## 4. Desarrollo local

### Requisitos

- [.NET SDK 8](https://dotnet.microsoft.com/download/dotnet/8.0)
- Node.js 24 (con npm)
- MySQL 8 local (por ejemplo, con Homebrew: `brew install mysql`) o un contenedor de Docker
- Opcional: VS Code con C# Dev Kit, TablePlus

### Backend

1. Crear una base vacía en el MySQL local (`CREATE DATABASE turnera;`). Las tablas las crea la API sola.
2. Crear `backend/TurneraJardin.Api/appsettings.Development.json` (está en `.gitignore`, **no se commitea**):

   ```json
   {
     "ConnectionStrings": {
       "Default": "Server=localhost;Port=3306;Database=turnera;User=root;Password=<tu-contraseña-local>"
     },
     "Jwt": {
       "Key": "<cualquier-texto-de-32-caracteres-o-mas-solo-para-desarrollo>"
     }
   }
   ```

   El email queda deshabilitado (`Email:Enabled=false`): los envíos se simulan y solo aparecen en la consola.

3. Correr:

   ```bash
   cd backend/TurneraJardin.Api
   dotnet run
   ```

   Queda en `http://localhost:5275`, con Swagger en `http://localhost:5275/swagger` (ver [`guia-swagger.md`](guia-swagger.md)).

La primera vez, con la base vacía, `DbSeeder` carga:

- **Docentes:** las de la sección `Seed:Teachers` de `appsettings.Development.json` (no está en el repo). Si no hay ninguna configurada, en Development crea tres docentes ficticias (`docenteN@example.com`). El repo ya no contiene datos personales reales.
- **Usuario `admin@jardin.edu.ar`:** con la contraseña de `Seed:AdminPassword` (mínimo 12 caracteres). Si no está configurada, genera una al azar y la escribe **una sola vez** en la consola al arrancar: cambiala desde "Mi cuenta".
- **Turnos de prueba** (solo en Development): lunes a viernes, 8 a 12, cada 20 minutos, por dos semanas.

Ejemplo de `appsettings.Development.json`:

```json
"Seed": {
  "AdminPassword": "una-contraseña-larga-local",
  "Teachers": [
    { "FirstName": "Ana", "LastName": "Pérez", "Email": "ana@ejemplo.com", "Classroom": "Maternal A", "Cycle": "FirstCycle" }
  ]
}
```

Para empezar de cero: borrar la base (`DROP DATABASE turnera; CREATE DATABASE turnera;`) y volver a correr.

### Frontend

```bash
cd frontend
npm install
npm start
```

Queda en `http://localhost:4200` y llama a la API en `http://localhost:5275/api` (`environment.ts`). Para verificar que compila igual que en Vercel: `npx ng build`.

### Tests

**Backend** (xUnit, `backend/TurneraJardin.Tests`):

```bash
cd backend
dotnet test
```

- Los tests unitarios corren siempre.
- Los de integración (`Integration/`) usan un MySQL real: crean una base descartable por test (`tj_test_...`) y la borran al terminar. Prueban migraciones (base nueva, base vieja sin historial, nombre de 48 caracteres como el de Railway), reserva con 10 familias a la vez, reintento de la misma familia, cancelación e historial. Sin la variable `TURNERA_TEST_MYSQL` se saltean. Para correrlos en local, con un usuario que pueda crear y borrar bases:

  ```bash
  TURNERA_TEST_MYSQL="Server=127.0.0.1;Port=3306;User=root;Password=<tu-contraseña-local>" dotnet test
  ```

**Frontend** (Vitest, con el builder `@angular/build:unit-test`):

```bash
cd frontend
npm test
```

Specs en `src/app/**/*.spec.ts`: teléfono y email (mismos casos que `PhoneNumberTests.cs` del backend), mensajes de error, `AuthService`, guards e interceptor.

**CI** (`.github/workflows/ci.yml`): en cada PR y push a `main` corre todo lo anterior. El backend usa un servicio MySQL 8.4 (misma versión que Railway), así que en GitHub los tests de integración no se saltean.

## 5. Base de datos

### Cómo se crea el esquema

- **EF Migrations** (desde el 12/11, carpeta `backend/TurneraJardin.Api/Migrations`). Al arrancar, `DbSeeder.Seed` aplica las migraciones pendientes. Una base creada antes con `EnsureCreated()` se marca en la migración base (`InitialBaseline`) sin ejecutarla. Detalle en [`migraciones-y-errores.md`](migraciones-y-errores.md).
- El seed solo carga datos si la tabla `Teachers` está vacía. Cambiar `DbSeeder.cs` no cambia los datos de una base que ya existe.

Tablas: `Teachers`, `Appointments`, `Users`, `AppointmentEvents` (historial) y `__EFMigrationsHistory`. Los enums se guardan como entero:

| Enum | Valores |
|---|---|
| `AppointmentStatus` | 0 Available, 1 Booked, 2 Cancelled, 3 Completed |
| `InterviewMode` | 0 InPerson, 1 Online |
| `TeacherCycle` | 1 FirstCycle, 2 SecondCycle, 3 Specialist (null = la familia elige) |
| `UserRole` | 0 Admin, 1 Teacher, 2 Coordinator |

Un valor nuevo de enum va siempre **al final**: reordenar cambiaría en silencio los datos guardados.

### ⚠️ Cambios de modelo

Cada columna nueva, renombre o cambio de tipo en un modelo necesita su migración:

```bash
cd backend
dotnet ef migrations add <NombreDescriptivo> --project TurneraJardin.Api
```

Revisar el archivo generado y commitearlo con el cambio de modelo. La migración se aplica sola en Railway al arrancar la versión nueva. Hacer backup antes de mergear una migración que borre o cambie columnas. El CI falla si el modelo cambió y falta la migración (`has-pending-model-changes`).

Antes de las migraciones había que hacer `ALTER TABLE` a mano (los scripts viejos están en `backend/scripts/`). Ya no hace falta.

### Entrar a la base de producción

Con la [CLI de Railway](https://docs.railway.com/guides/cli) instalada:

```bash
railway login
railway link              # proyecto laudable-charisma, entorno production
railway connect MySQL     # abre una consola mysql
```

Para usar TablePlus u otra herramienta: `railway connect MySQL --tunnel-only` y conectarse al host y puerto que muestra.

- El túnel usa SSH: con VPN activa puede no abrir. Si se corta, volver a conectar.
- Antes de cualquier `UPDATE`, `DELETE` o `ALTER`, hacer backup.

### Backup y restauración

Con el túnel abierto (`--tunnel-only`) en otra terminal:

```bash
mysqldump -h 127.0.0.1 -P <puerto-del-túnel> -u root -p <base> > backup-$(date +%F).sql
```

`<base>` es el valor de `MYSQLDATABASE` en las variables del servicio MySQL de Railway. Para restaurar: `mysql -h 127.0.0.1 -P <puerto> -u root -p <base> < backup.sql`.

Los backups tienen datos personales de familias: no se suben al repo ni se comparten por chats.

## 6. Cómo se despliega un cambio

`main` es la única rama permanente. Railway y Vercel despliegan solos cada push a `main`.

1. Rama nueva desde `main` actualizado: `git checkout main && git pull && git checkout -b <tipo>/<descripcion>`.
2. Probar en local: `dotnet test` en `backend/`, `npm test` y `npx ng build` en `frontend/`.
3. Push y Pull Request con base `main`.
4. Si el cambio toca un modelo: agregar la migración (sección 5) y hacer backup de Railway antes de mergear.
5. Mergear y esperar los dos deploys (Railway tarda unos minutos más que Vercel).
6. Probar en producción la parte tocada.
7. Borrar la rama (local y remota) y volver a `main`.

Lecciones aprendidas:
- Cambiar de rama **antes** de editar archivos.
- Un commit pusheado a una rama después de mergear su PR no llega a `main`: necesita un PR nuevo.
- La versión de Git que trae macOS puede no tener `git switch`: usar `git checkout -b`.

## 7. Emails (Brevo)

| Cuándo | Asunto | Contenido |
|---|---|---|
| La familia reserva | "Turno confirmado: …" | Niño/a, docente, fecha, horario, modalidad, número de turno y link a `/cancelar` |
| El jardín cancela desde el panel | "Turno cancelado: …" | Datos del turno y link para reservar de nuevo |

- Se envían por la API HTTPS de Brevo porque Railway bloquea SMTP en sus planes gratuitos.
- Si el envío falla, **la reserva o cancelación se hace igual** y queda un warning en el log. En el panel, la columna "Avisos" muestra "Email no enviado".
- Cuando la familia cancela sola, no se manda email.
- Con `Email:Enabled=false` (desarrollo) no se envía nada: se registra en la consola.
- El plan gratuito de Brevo tiene un tope diario de envíos: revisarlo en la cuenta si el volumen crece.

## 8. WhatsApp

- **Confirmación de asistencia (en uso):** el botón **Confirmación** del listado arma un link `https://wa.me/<teléfono>?text=…` con un mensaje para la familia y lo abre en el WhatsApp de quien lo toca (app en el celular, WhatsApp Web en la compu). El sistema solo registra cuándo se pidió (`ConfirmationRequestedAt`); la respuesta llega a ese chat. Sin costo.
- **Teléfonos de contacto del jardín:** uno por turno, configurados en `frontend/src/app/core/config/school-contact.ts` (ver sección 11).
- **Cloud API de Meta (sin uso):** `WhatsAppCloudApiService` y `ReminderBackgroundService` siguen en el código, pero el servicio de recordatorios no está registrado en `Program.cs` y `WhatsApp:Enabled=false`. Se descartó porque Meta cobra por mensaje de plantilla fuera de una conversación abierta por el cliente, y requiere verificar la empresa y aprobar plantillas.

## 9. Reglas de negocio

| Regla | Dónde se aplica |
|---|---|
| Se reserva a partir del día siguiente (hora de Argentina; Railway corre en UTC, por eso existe `ArgentinaDate`) | Backend (`AppointmentsController.Book`) |
| Se reserva hasta 180 días hacia adelante | Backend (`ArgentinaDate.BookingDaysAhead`) y frontend (`BOOKING_DAYS_AHEAD`): mantener iguales |
| Primera entrevista del niño/a: presencial; las siguientes: virtual. La familia lo indica en el paso 3, igual para todos los ciclos | Backend y frontend |
| Dos familias no pueden tomar el mismo turno | `Book` es un único `UPDATE … WHERE Status = Available` (la segunda recibe 409). Si la misma familia reintenta (mismo teléfono y email), recibe 200 con la misma reserva |
| Una docente desactivada no aparece en la reserva pública, no se le puede reservar ni generar turnos | Backend |
| Teléfonos: se guardan normalizados como `549` + característica + número (acepta `0223 15 555-1234`, `+54 9 …`, `(223) …`) | Backend (`Services/PhoneNumber.cs`) y frontend (`shared/phone.util.ts`): misma lógica |
| Email: tiene que tener dominio con punto (`juan@gmail` no se acepta) | Backend (`StrictEmail`) y frontend |
| La familia cancela con número de turno + teléfono (en el cuerpo del pedido, cualquier formato del mismo número). "No existe" y "el teléfono no coincide" responden igual (404) | Backend |
| La familia no puede cancelar desde la web un turno de hoy o de días anteriores | Backend |
| Cancelar (familia o panel) libera el turno como Disponible y borra los datos de la familia. Desde el panel, primero se guarda y después se avisa por email | Backend |
| Desde el panel, "Eliminar" solo borra turnos Disponibles; un turno reservado se cancela | Backend |
| El borrado en bloque borra por lista de ids y **solo** los Disponibles; los reservados se saltean y se informan | Backend (`POST /api/admin/appointments/bulk-delete`) |
| Generar turnos: hasta 6 meses por pedido, máximo 5.000 turnos por pedido, desde hoy, nunca cruza la medianoche. No se duplican horarios ya cargados (si dos coordinadoras generan a la vez, se reintenta una vez) | Backend; el formulario muestra cuántos turnos por día salen y si sobran minutos |
| Si dos personas modifican el mismo turno a la vez, la segunda recibe 409 (Status y BookedAt son tokens de concurrencia) | Backend |
| Límites por IP: reservar y cancelar, 20 cada 10 minutos; lecturas públicas, 120 por minuto; login, 10 cada 5 minutos. Además, 5 logins fallidos seguidos bloquean ese email 15 minutos. El 429 trae `Retry-After` | Backend (`Program.cs`, `LoginAttemptTracker`) |
| La sesión del panel dura 8 horas, pero se corta en el acto si el usuario se desactiva, cambia o le restablecen la contraseña, o cambia su rol o docente. El frontend, ante un 401, cierra sesión y vuelve al login | Backend (`OnTokenValidated` + `SecurityStamp`), frontend (`auth.interceptor.ts`) |
| Un usuario Docente sin docente asociada no ve nada (403) | Backend (`TryGetScope`) |
| Contraseñas de al menos 8 caracteres | Backend y frontend |

## 10. Referencia de la API

Los marcados 🔒 necesitan `Authorization: Bearer <token>` (se obtiene en `/api/auth/login`). Todos los errores traen `message` en español: los de negocio como `{ "message": "…" }`, los de validación y los inesperados como ProblemDetails con `message` adentro. Conflictos de base (clave duplicada, cambio concurrente) → 409.

| Método | Ruta | Quién | Descripción |
|---|---|---|---|
| POST | `/api/auth/login` | Público | Login (límite `login`) |
| POST 🔒 | `/api/auth/change-password` | Cualquier usuario | Cambia la contraseña propia y devuelve una sesión nueva (las demás quedan invalidadas) |
| POST 🔒 | `/api/auth/my-photo` | Cualquier usuario | Sube foto de perfil (campo `file`; JPG/PNG/WEBP, máx. 2 MB) |
| GET | `/api/teachers` | Público | Docentes activas |
| GET | `/api/teachers/{id}/available-appointments?from&to` | Público | Turnos libres (por defecto desde mañana) |
| GET | `/api/appointments/available?teacherId&date` | Público | Turnos libres de una docente (teacherId obligatorio) |
| POST | `/api/appointments/{id}/book` | Público | Reserva; el email sale en segundo plano (límite `public`) |
| POST | `/api/appointments/{id}/cancel` | Público | Cancela validando el teléfono, enviado en el cuerpo `{ "phone": "…" }` (límite `public`; `?phone=` se acepta temporalmente) |
| GET | `/health` | Público | 200 si la API llega a MySQL |
| GET 🔒 | `/api/admin/teachers` | Admin, Coordinador | Todas las docentes |
| POST 🔒 | `/api/admin/teachers` | Admin, Coordinador | Alta |
| PUT 🔒 | `/api/admin/teachers/{id}` | Admin, Coordinador | Edición |
| DELETE 🔒 | `/api/admin/teachers/{id}` | Admin, Coordinador | Desactiva (no borra) |
| POST 🔒 | `/api/admin/teachers/{id}/generate-appointments` | Admin, Coordinador | Genera turnos → `{ created, skippedExisting }` |
| GET 🔒 | `/api/admin/appointments?teacherId&from&to&status` | Todos los roles | Lista (una docente solo ve los suyos) |
| POST 🔒 | `/api/admin/appointments/{id}/cancel` | Todos (docente: solo propios) | Cancela y avisa por email |
| POST 🔒 | `/api/admin/appointments/{id}/request-confirmation` | Todos (docente: solo propios) | Registra el pedido de confirmación |
| DELETE 🔒 | `/api/admin/appointments/{id}` | Todos (docente: solo propios) | Elimina un turno Disponible |
| POST 🔒 | `/api/admin/appointments/bulk-delete` | Admin, Coordinador | Borra los Disponibles de `{ "ids": [...] }` → `{ deleted, skippedBooked }` |
| GET 🔒 | `/api/admin/users` | Admin | Usuarios del panel |
| POST 🔒 | `/api/admin/users` | Admin | Crea un acceso (docente, coordinador o admin) |
| POST 🔒 | `/api/admin/users/{id}/reset-password` | Admin | Genera contraseña temporal |
| PUT 🔒 | `/api/admin/users/{id}/activate` | Admin | Reactiva |
| POST 🔒 | `/api/admin/users/{id}/deactivate` | Admin | Desactiva (no deja desactivar al último usuario de dirección) |

Permisos en detalle: [`roles-y-permisos.md`](roles-y-permisos.md).

La pantalla **Usuarios** del panel solo crea accesos para docentes. Un usuario **Coordinador** o **Admin** se crea por API (`POST /api/admin/users` con `role`), por ejemplo desde Swagger en local apuntando a la base correspondiente, o con un pedido HTTP autenticado contra producción.

## 11. Configuración que vive en el código

Estos valores no son variables de entorno: cambiarlos requiere commit y deploy.

| Qué | Archivo |
|---|---|
| Teléfonos de contacto del jardín (mañana y tarde) | `frontend/src/app/core/config/school-contact.ts` |
| Ventana de días para reservar (180) | `backend/.../Services/ArgentinaDate.cs` → `BookingDaysAhead` y `frontend/src/app/core/services/booking.service.ts` → `BOOKING_DAYS_AHEAD` (iguales) |
| URL de la API | `frontend/src/environments/environment*.ts` |
| Preguntas frecuentes de la reserva | `frontend/src/app/public-booking/select-teacher/select-teacher.component.html` |
| Texto del WhatsApp de confirmación | `appointments-list.component.ts` → `confirmationMessage()` |
| Textos de los emails | `backend/.../Services/BrevoEmailService.cs` |
| Límites de pedidos por IP | `backend/.../Program.cs` |

## 12. Seguridad

- Secretos de producción (`Jwt__Key`, API key de Brevo, contraseña de MySQL) **solo en Railway**. `appsettings.json` los tiene vacíos y `appsettings.Development.json` está en `.gitignore`.
- Contraseña del admin y de MySQL de producción ya cambiadas respecto de las iniciales.
- `gitleaks detect -v` (23/10): 12 hallazgos, todos claves y contraseñas de **desarrollo** que quedaron en el historial (distintas de las de producción). No se reescribió el historial. Correrlo de nuevo antes de cada entrega.
- Auditorías (12/11, Angular 22): `dotnet list package --vulnerable` sin hallazgos; `npm audit` con 0 vulnerabilidades, incluidas las de desarrollo. Karma se reemplazó por Vitest.
- Las fotos de perfil se validan por tipo y tamaño.

## 13. Problemas conocidos y cómo resolverlos

| Síntoma | Causa probable | Qué hacer |
|---|---|---|
| El navegador muestra "CORS Missing Allow Origin" | Casi siempre es un **500** del backend (un error no lleva headers CORS) | Ver el log del servicio en Railway. Si dice `Unknown column`, falta un `ALTER TABLE` (sección 5) |
| La API no arranca en Railway | Falta `ConnectionStrings__Default` o `Jwt__Key` < 32 caracteres | Revisar variables (sección 3) |
| La familia reserva pero no llega el email | API key o remitente de Brevo, o tope diario | Log de Railway y panel de Brevo; revisar spam |
| La familia no puede cancelar ("El teléfono no coincide") | Escribió el número distinto de como lo cargó (por ejemplo, con 0 o 15) | Que lo escriba igual que al reservar, o cancelar desde el panel |
| `dotnet run`: "address already in use" | Quedó otra instancia en el puerto 5275 | `lsof -nP -iTCP:5275 -sTCP:LISTEN` y `kill -9 <PID>` |
| `railway connect` no abre | VPN activa o túnel cortado | Apagar la VPN y reconectar |
| Una docente nueva cargada en `DbSeeder` no aparece en producción | El seed no corre si ya hay docentes | Cargarla desde el panel (Docentes → Nuevo docente) |

## 14. Pendientes

- Decidir el hosting a largo plazo y hacer backup antes de migrar.
- Prueba completa en producción después de la traducción al inglés y limpieza de variables viejas en Railway.
- Backups periódicos de la base.
- Tope de anticipación de reserva también en el backend.
- "Olvidé mi contraseña" sin estar logueado.
- Email a la familia cuando cancela ella misma.
- Definir cómo recibe la familia el link de la videollamada en entrevistas virtuales.
- Registrar la respuesta de la familia a la confirmación (confirmó / no asiste).
- Editar el teléfono de la familia en un turno reservado; normalizar números con "15".
- Tests de componentes del frontend (hoy hay specs de utilidades, servicio de sesión, guards e interceptor).
