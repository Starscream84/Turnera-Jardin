> 📌 **Documento interno del equipo** — guía completa de instalación, configuración y decisiones técnicas. Para la presentación del proyecto ver el [`README.md`](./README.md) principal del repositorio.

# Turnera Web — Jardín Maternal (guía interna)

Sistema de reserva de turnos de entrevista para un jardín maternal. Los padres reciben un link (por WhatsApp) y sacan un turno de entrevista con el/la docente de su hijo/a, sin llamar ni escribir a nadie. El sistema manda la confirmación y un recordatorio un día antes, también por WhatsApp.

Hecho para donarlo a la institución como alternativa a Agenda Pro u otro sistema pago.

## Índice

1. [Qué incluye el proyecto](#qué-incluye-el-proyecto)
2. [Arquitectura y stack](#arquitectura-y-stack)
3. [Estructura de carpetas](#estructura-de-carpetas)
4. [Prerrequisitos](#prerrequisitos)
5. [Cómo correr el backend](#cómo-correr-el-backend)
6. [Cómo correr el frontend](#cómo-correr-el-frontend)
7. [Datos y credenciales de prueba](#datos-y-credenciales-de-prueba)
8. [Configurar WhatsApp (Cloud API de Meta)](#configurar-whatsapp-cloud-api-de-meta)
9. [⚠️ Sobre el costo de WhatsApp — leer antes de decidir](#️-sobre-el-costo-de-whatsapp--leer-antes-de-decidir)
10. [Cómo funciona el sistema, paso a paso](#cómo-funciona-el-sistema-paso-a-paso)
11. [Referencia de la API](#referencia-de-la-api)
12. [Decisiones de diseño y por qué](#decisiones-de-diseño-y-por-qué)
13. [Limitaciones conocidas y qué falta para producción](#limitaciones-conocidas-y-qué-falta-para-producción)
14. [Opciones de hosting gratuito para desplegarlo](#opciones-de-hosting-gratuito-para-desplegarlo)

---

## Qué incluye el proyecto

- **`backend/`** — API en C# / ASP.NET Core 8 con Entity Framework Core y SQLite (no requiere instalar ningún motor de base de datos aparte).
- **`frontend/`** — Aplicación en Angular 19 (standalone components) con dos partes:
  - **Flujo público**: elegir docente → ver horarios libres → completar datos → confirmar turno. Sin login.
  - **Panel de administración**: login, ABM de los 18 docentes, generación de turnos disponibles y listado/cancelación de reservas.
- Envío de WhatsApp de **confirmación** (al reservar) y **recordatorio** (un día antes) usando la **WhatsApp Cloud API** oficial de Meta.

Importante: en el sandbox donde armé este proyecto, el acceso a NuGet (el repositorio de paquetes de .NET) estaba bloqueado por una política de red, así que **no pude compilar ni correr el backend acá**. Lo escribí con cuidado siguiendo los patrones estándar de ASP.NET Core, y el frontend en Angular sí lo compilé y probé (`ng build` y `ng serve` corrieron sin errores). Cuando lo abras en tu máquina con conexión a internet normal, `dotnet restore` debería bajar los paquetes sin problema — si aparece algún error de compilación puntual, avisame y lo corregimos.

## Arquitectura y stack

```
┌─────────────────┐        HTTP/JSON        ┌──────────────────────┐
│  Angular (SPA)   │ ───────────────────────▶│  ASP.NET Core Web API │
│  localhost:4200   │◀─────────────────────── │  localhost:5275       │
└─────────────────┘                          └──────────┬───────────┘
                                                          │ EF Core
                                                          ▼
                                                ┌───────────────────┐
                                                │  SQLite (turnera.db) │
                                                └───────────────────┘
                                                          │
                                                          ▼
                                            ┌──────────────────────────┐
                                            │ WhatsApp Cloud API (Meta) │
                                            └──────────────────────────┘
```

- **Backend**: ASP.NET Core 8 Web API + Entity Framework Core 8 + SQLite + autenticación JWT.
- **Frontend**: Angular 19, standalone components, Reactive Forms, signals. Sin librerías de UI de terceros (CSS propio, liviano).
- **Base de datos**: SQLite — un solo archivo (`turnera.db`), cero configuración, cero costo.
- **Mensajería**: WhatsApp Cloud API (Meta), vía HTTP. Ver la sección de configuración y de costos más abajo.

## Estructura de carpetas

```
turnera-jardin/
├── README.md                      (este archivo)
├── backend/
│   ├── TurneraJardin.sln
│   └── TurneraJardin.Api/
│       ├── Controllers/           (endpoints HTTP)
│       ├── Models/                (entidades EF Core)
│       ├── Dtos/                  (objetos de entrada/salida de la API)
│       ├── Data/                  (DbContext y datos de ejemplo)
│       ├── Services/              (JWT, WhatsApp, job de recordatorios)
│       ├── appsettings.json       (configuración: JWT, WhatsApp, CORS)
│       └── Program.cs
└── frontend/
    └── src/app/
        ├── core/                  (servicios HTTP, modelos, guard, interceptor)
        ├── public-booking/        (flujo público de reserva)
        └── admin/                 (panel de administración)
```

## Prerrequisitos

Para correr el proyecto en tu máquina necesitás instalar (todo gratuito):

- [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20 o superior](https://nodejs.org/) (incluye npm)
- [Visual Studio Code](https://code.visualstudio.com/) con la extensión **C# Dev Kit** (para el backend) — opcional pero recomendado
- Angular CLI: `npm install -g @angular/cli` (opcional, el proyecto ya trae los scripts de npm para no necesitarlo instalado globalmente)

## Cómo correr el backend

```bash
cd backend/TurneraJardin.Api
dotnet restore
dotnet run
```

La primera vez que corre, la API:

1. Crea el archivo `turnera.db` (SQLite) en esa misma carpeta.
2. Carga automáticamente datos de ejemplo: **18 docentes**, un usuario administrador, y turnos disponibles para las próximas dos semanas (lunes a viernes, 8 a 12, cada 20 minutos) para que puedas probar todo el flujo sin cargar nada a mano.

La API queda escuchando en **`http://localhost:5275`**. Con `dotnet run`, en modo Development se abre automáticamente Swagger (`http://localhost:5275/swagger`) donde podés ver y probar todos los endpoints.

> Para volver a empezar de cero, simplemente borrá el archivo `turnera.db` y volvé a correr `dotnet run`.

### Abrir el backend en VS Code

1. Abrí la carpeta `backend/` en VS Code.
2. Instalá la extensión **C# Dev Kit** si no la tenés.
3. Con el archivo `Program.cs` abierto, apretá F5 (o "Run and Debug") para correr con el depurador conectado.

## Cómo correr el frontend

```bash
cd frontend
npm install
npm start
```

Esto levanta el frontend en **`http://localhost:4200`**, apuntando por defecto a la API en `http://localhost:5275/api` (configurado en `src/environments/environment.ts`). Andá a `http://localhost:4200` y ya deberías ver la pantalla para elegir docente.

### Abrir el frontend en VS Code

Abrí la carpeta `frontend/` en VS Code (podés tener las dos carpetas —`backend` y `frontend`— abiertas en dos ventanas, o abrir la carpeta raíz `turnera-jardin/` y trabajar con ambas desde el explorador de archivos).

## Datos y credenciales de prueba

El seed inicial crea este usuario administrador:

| Email | Contraseña |
|---|---|
| `admin@jardin.edu.ar` | `CambiarEsta123!` |

**Cambiá esta contraseña antes de usar el sistema con datos reales.** Podés hacerlo vos mismo desde **Panel → Mi contraseña**, sin tocar la base de datos.

Los 18 docentes de ejemplo (Ana Gómez, Bruno Rodríguez, etc.) los podés editar o reemplazar por los reales desde **Panel → Docentes**.

Para que cada docente pueda entrar al panel y ver *solo sus propios turnos*, dirección le crea un login individual desde **Panel → Usuarios**: se elige el docente, se genera (o se define) una contraseña temporal, y se le pasa a la persona por un medio seguro para que la cambie apenas entre. Desde esa misma pantalla dirección puede restablecer la contraseña de cualquier usuario (propio o de un docente) cuando haga falta, y activar/desactivar accesos.

## Configurar WhatsApp (Cloud API de Meta)

Sin esto configurado, el sistema funciona igual (se pueden reservar y cancelar turnos), pero los mensajes de WhatsApp no se mandan: solo quedan registrados en el log de la consola (`WhatsApp:Habilitado` está en `false` por defecto en `appsettings.json`). Esto es útil para desarrollar y probar sin tener que configurar nada de Meta todavía.

Pasos para activarlo de verdad:

1. **Creá una cuenta en [Meta for Developers](https://developers.facebook.com/)** con el Facebook de la institución (o uno nuevo dedicado al jardín).
2. Creá una app nueva de tipo **"Business"**, y agregale el producto **WhatsApp**.
3. En la configuración de WhatsApp de la app vas a encontrar:
   - Un **número de prueba** gratuito que Meta te da automáticamente (sirve para probar mandando mensajes a hasta 5 números que vos autorices, sin verificar nada más). Es el mejor punto de partida.
   - El **Phone Number ID** de ese número (un número largo, lo vas a necesitar).
   - Un **token de acceso temporal** (dura 24hs, sirve solo para probar).
4. Para producción real (que le llegue a cualquier familia, no solo a los 5 números de prueba), tenés que:
   - Verificar un número de teléfono real de la institución como número de WhatsApp Business.
   - Verificar el **negocio** en Meta Business Manager (sube documentación de la institución).
   - Generar un **token de acceso permanente** (con un "System User" en Meta Business Manager, no el token temporal de 24hs).
5. **Crear las dos plantillas de mensaje** (*message templates*) que usa el sistema, en Meta Business Manager → WhatsApp Manager → Plantillas de mensaje. Tienen que aprobarse (Meta las revisa, normalmente en minutos u horas):

   **Plantilla `confirmacion_turno`** (categoría *Utility*, idioma español (ARG)):
   > Hola! Confirmamos el turno de entrevista para {{1}} con {{2}}, el día {{3}} a las {{4}} hs. Ante cualquier duda, respondé este mensaje.

   Donde `{{1}}`=nombre del niño/a, `{{2}}`=nombre del docente, `{{3}}`=fecha, `{{4}}`=hora.

   **Plantilla `recordatorio_turno`** (categoría *Utility*, idioma español (ARG)):
   > ¡Hola! Te recordamos que mañana tenés turno de entrevista para {{1}} con {{2}} a las {{3}} hs. Te esperamos.

   Donde `{{1}}`=nombre del niño/a, `{{2}}`=nombre del docente, `{{3}}`=hora.

6. Cargá los datos obtenidos en `backend/TurneraJardin.Api/appsettings.json` (o, mejor, en variables de entorno o en `dotnet user-secrets` para no dejar el token en el código fuente):

   ```json
   "WhatsApp": {
     "Habilitado": true,
     "AccessToken": "el-token-permanente-acá",
     "PhoneNumberId": "el-phone-number-id-acá",
     "ApiVersion": "v20.0",
     "TemplateConfirmacion": "confirmacion_turno",
     "TemplateRecordatorio": "recordatorio_turno",
     "CodigoIdiomaTemplate": "es_AR"
   }
   ```

   Con `dotnet user-secrets` (recomendado para no commitear el token):
   ```bash
   cd backend/TurneraJardin.Api
   dotnet user-secrets init
   dotnet user-secrets set "WhatsApp:AccessToken" "tu-token-acá"
   dotnet user-secrets set "WhatsApp:PhoneNumberId" "tu-phone-number-id-acá"
   ```

## ⚠️ Sobre el costo de WhatsApp — leer antes de decidir

Pediste que el sistema no requiera contratar ningún servicio pago, así que quiero ser bien directo con esto en vez de asumir que la Cloud API sale gratis, porque **la política de precios de Meta cambió** y sigue cambiando:

- Hasta julio de 2025, WhatsApp cobraba por "conversación" (una ventana de 24hs). Desde el 1 de julio de 2025, Meta pasó a cobrar **por mensaje individual** entregado.
- Los mensajes de plantilla ("template", que es lo que este sistema usa para mandar la confirmación y el recordatorio, porque el padre no le escribe primero al jardín) son **gratis únicamente si se mandan dentro de una "ventana de conversación" ya abierta por el cliente** (es decir, si el padre le escribió antes al número del jardín en las últimas 24hs). Fuera de esa ventana — que es el caso normal acá, porque el padre reserva desde la web sin escribirle antes al WhatsApp — **la plantilla se cobra**, a una tarifa que varía por país y por categoría del mensaje (la nuestra es "Utility", la más económica de las que se cobran).
- Hay cambios de tarifas previstos para el 1° de octubre de 2026 en varios países (li mismo Meta lo aclara en su documentación oficial), así que la tarifa exacta puede moverse.
- El número de prueba gratuito de Meta (para desarrollo) sí permite mandar mensajes sin cargo, pero solo a números que vos autorizaste a mano (máximo 5) — no sirve para producción con familias reales.

**En criollo**: con el flujo tal como está armado (el padre reserva desde la web, no le escribe primero al jardín), lo más probable es que los mensajes de confirmación y recordatorio **tengan un costo por mensaje**, aunque en general es un costo bajo (centavos de dólar por mensaje según el país) y el volumen de un jardín maternal es chico (unas decenas de turnos por mes, dos mensajes cada uno). No es gratis en el sentido estricto que pediste, pero tampoco es una suscripción cara.

Opciones si esto es un problema:

1. **Aceptar el costo bajo por mensaje** — probablemente la opción más simple; revisen el costo real para Argentina en el [simulador de precios de Meta](https://developers.facebook.com/documentation/business-messaging/whatsapp/pricing) antes de decidir, porque no puedo confirmarles un número exacto y actualizado desde acá.
2. **Pedirle al padre que le escriba primero al WhatsApp del jardín** (por ejemplo, con un botón "Avisar por WhatsApp" que abre un link `wa.me` con un mensaje precargado tipo "Hola, guardé mi turno"). Eso abre una ventana de conversación con el cliente, y la confirmación (si se manda dentro de esa ventana) sale gratis. El recordatorio del día siguiente, en cambio, casi seguro va a quedar fuera de esa ventana igual, así que ese sí se cobraría. No lo implementé porque agrega un paso manual para el padre y no estaba en el pedido original, pero es una alternativa válida si quieren reducir el costo.
3. **Usar una librería no oficial** (whatsapp-web.js / Baileys), que ya habíamos descartado al planificar esto porque viola los términos de servicio de WhatsApp y el número puede terminar bloqueado — no la recomiendo para una institución real, pero la dejo mencionada como opción 100% gratis si prefieren asumir ese riesgo.

El código ya está armado para la opción 1 (Cloud API con plantillas), que es la más prolija y estable. Si prefieren otra, avisen y la adaptamos.

**Fuentes consultadas** (septiembre de 2026):
- [Documentación oficial de precios — Meta for Developers](https://developers.facebook.com/documentation/business-messaging/whatsapp/pricing)
- [WhatsApp API Pricing 2026 — Peppercloud](https://blog.peppercloud.com/whatsapp-api-pricing-everything-you-need-to-know/)
- [WhatsApp Pricing Change 2026 — respond.io](https://respond.io/blog/whatsapp-pricing-change-2026)

## Cómo funciona el sistema, paso a paso

**Flujo de la familia:**
1. Recibe por WhatsApp un link al sitio (ej: `https://turnos.jardin.edu.ar/reservar`).
2. Elige el/la docente de su hijo/a.
3. Ve los horarios libres agrupados por día y elige uno.
4. Completa su nombre, WhatsApp, y el nombre del niño/a.
5. Confirma. El turno queda reservado al instante (no requiere aprobación) y recibe la confirmación por WhatsApp.
6. Un día antes de la entrevista, recibe el recordatorio automático.
7. Si necesita cancelar, entra a `/cancelar`, pone el número de turno (se lo mostramos al confirmar) y su WhatsApp.

**Flujo de dirección / docentes:**
1. Dirección entra a `/admin/login` con su usuario y contraseña.
2. Desde **Docentes**, da de alta/edita/desactiva a los 18 docentes.
3. Desde **Generar turnos**, elige un docente, un rango de fechas, los días de la semana, el horario y la duración de cada entrevista — el sistema genera todas las franjas automáticamente.
4. Desde **Usuarios**, dirección crea el login individual de cada docente y restablece contraseñas cuando haga falta (ver sección anterior).
5. Desde **Turnos**, dirección ve todas las reservas (y cada docente, si tiene su propio usuario, solo ve las suyas); puede cancelar reservas o eliminar franjas libres cargadas por error.
6. Cualquier usuario logueado puede cambiar su propia contraseña desde **Mi contraseña**.
7. Cada 30 minutos, un proceso interno revisa si hay turnos para el día siguiente sin recordatorio enviado, y lo manda.

## Referencia de la API

Todos los endpoints devuelven/reciben JSON. Los marcados 🔒 requieren el header `Authorization: Bearer <token>` obtenido en `/api/auth/login`.

| Método | Ruta | Descripción |
|---|---|---|
| POST | `/api/auth/login` | Login de dirección/docentes |
| GET | `/api/docentes` | Lista pública de docentes activos |
| GET | `/api/docentes/{id}/turnos-disponibles` | Horarios libres de un docente |
| POST | `/api/turnos/{id}/reservar` | Reserva un turno (manda WhatsApp de confirmación) |
| POST | `/api/turnos/{id}/cancelar?telefono=` | Cancela un turno (valida el teléfono) |
| GET 🔒 | `/api/admin/docentes` | Lista completa de docentes (solo Admin) |
| POST 🔒 | `/api/admin/docentes` | Crea un docente (solo Admin) |
| PUT 🔒 | `/api/admin/docentes/{id}` | Edita un docente (solo Admin) |
| DELETE 🔒 | `/api/admin/docentes/{id}` | Desactiva un docente (solo Admin) |
| POST 🔒 | `/api/admin/docentes/{id}/generar-turnos` | Genera turnos en bloque (solo Admin) |
| GET 🔒 | `/api/admin/turnos` | Lista turnos (filtra por docente/fecha/estado; un docente solo ve los suyos) |
| POST 🔒 | `/api/admin/turnos/{id}/cancelar` | Cancela una reserva |
| DELETE 🔒 | `/api/admin/turnos/{id}` | Elimina una franja libre |
| GET 🔒 | `/api/admin/usuarios` | Lista todos los usuarios del panel (solo Admin) |
| POST 🔒 | `/api/admin/usuarios` | Crea un login (de un docente, o de otro Admin) (solo Admin) |
| POST 🔒 | `/api/admin/usuarios/{id}/restablecer-password` | Restablece la contraseña de un usuario (solo Admin) |
| PUT 🔒 | `/api/admin/usuarios/{id}/activar` | Reactiva un acceso (solo Admin) |
| POST 🔒 | `/api/admin/usuarios/{id}/desactivar` | Desactiva un acceso (solo Admin) |
| POST 🔒 | `/api/auth/cambiar-password` | Cualquier usuario logueado cambia su propia contraseña |

La documentación interactiva completa (Swagger) está disponible corriendo el backend y entrando a `http://localhost:5275/swagger`.

## Decisiones de diseño y por qué

- **SQLite en vez de SQL Server**: cero instalación, cero costo, un solo archivo — ideal para que el jardín no tenga que mantener un servidor de base de datos.
- **Turnos como una sola tabla** (en vez de "disponibilidad" + "reservas" separadas): simplifica mucho el modelo. Una franja horaria "vacía" (`Estado = Disponible`) se convierte en una reserva completando los mismos campos, en vez de tener dos tablas relacionadas.
- **JWT con roles Admin/Docente**: cada docente puede tener su propio usuario (creado por dirección desde **Panel → Usuarios**) y ver solo su agenda, sin exponerle las de sus compañeros — el filtro por `docenteId` está aplicado en `AdminTurnosController`. El seed solo crea el usuario Admin inicial; los accesos de cada docente se cargan desde el panel, no hace falta tocar la base.
- **Transacción al reservar**: evita que dos familias reserven el mismo horario si tocan "confirmar" casi al mismo tiempo.
- **El envío de WhatsApp nunca bloquea la reserva**: si WhatsApp falla (por la razón que sea), el turno queda igual reservado, y se guarda un flag (`confirmacionEnviada = false`) para poder detectar el problema después. No quisimos que una falla de WhatsApp le impida a la familia sacar el turno.
- **El recordatorio corre dentro del mismo proceso de la API** (`BackgroundService`), revisando cada 30 minutos. Es la opción más simple para un proyecto de este tamaño; no requiere infraestructura extra. La contra es que si el proceso de la API está caído justo en ese momento, el recordatorio se manda en cuanto vuelve a levantar (no se pierde, solo se atrasa).

## Limitaciones conocidas y qué falta para producción

- No hay envío de email, solo WhatsApp.
- El recordatorio depende de que el proceso de la API esté corriendo 24/7. Para un despliegue real conviene alojarlo en un servicio que lo mantenga siempre activo (ver sección de hosting).
- No hay política de reintento automático si falla el envío de un WhatsApp (queda el flag `confirmacionEnviada`/`recordatorioEnviado` en false, pero nadie lo reintenta solo — se puede agregar un botón "reenviar" en el panel).
- No hay recuperación de contraseña para un usuario que la olvidó sin estar logueado ("olvidé mi contraseña" desde la pantalla de login) — hoy, si un docente se olvida la suya, dirección se la restablece desde **Panel → Usuarios**.
- No se implementó recuperación de contraseña ("olvidé mi contraseña").
- La app no compila ni se probó de punta a punta con WhatsApp real (no se puede sin una cuenta de Meta Business verificada); sí se probó que compila y corre el frontend, y el backend fue revisado a mano con mucho cuidado pero no compilado en este entorno (ver nota al principio del documento).

## Opciones de hosting gratuito para desplegarlo

Este README se centra en correrlo local en VS Code, como pediste, pero si más adelante quieren que el jardín lo use de verdad (no solo en tu máquina), estas son opciones sin costo para alojar cada parte (dejo la mención nomás, no las configuré):

- **Backend (.NET)**: [Render](https://render.com) o [Railway](https://railway.app) tienen planes gratuitos para apps pequeñas; Azure también tiene un nivel gratuito para App Service (con limitaciones de uso).
- **Frontend (Angular, sitio estático)**: [Netlify](https://netlify.com), [Vercel](https://vercel.com) o [GitHub Pages](https://pages.github.com) — todos gratuitos para un sitio de este tamaño.
- **Base de datos**: si migran de SQLite a algo con más capacidad de escritura simultánea, [Turso](https://turso.tech) (SQLite en la nube) o [Supabase](https://supabase.com) (Postgres) tienen niveles gratuitos.

Si en algún momento quieren que arme el despliegue real a alguno de estos servicios, lo vemos en otra sesión.
