# Migraciones, errores de la API y logs

Cambios del 12/11/2026 (pendientes del informe de QA: H06, H10, H12, H20, H21, H23, H30, H31).

## 1. EF Core Migrations

El esquema de la base ya no se crea con `EnsureCreated()`: lo definen las migraciones de
`backend/TurneraJardin.Api/Migrations/`, y la API las aplica sola al arrancar (`DbSeeder.ApplyMigrations`).

| Migración | Qué hace |
|---|---|
| `20261112113341_InitialBaseline` | El esquema tal como estaba al pasar a migraciones (Teachers, Users, Appointments e índices). |
| `20261112114020_HistoryAndPasswordChange` | `Users.MustChangePassword` y la tabla `AppointmentEvents` (historial). |
| `20261119120000_SingleNameFields` | Nombre y apellido en un solo campo: `Appointments.ChildName` y `GuardianName` reemplazan a `ChildFirstName`/`ChildLastName` y `GuardianFirstName`/`GuardianLastName`. Une los datos existentes antes de borrar las columnas viejas. |

**Bases creadas antes (Railway, bases locales del grupo).** Si la API encuentra las tablas pero no la tabla
`__EFMigrationsHistory`, marca la baseline como aplicada **sin ejecutarla** y después corre solo las
migraciones nuevas. No hace falta tocar nada a mano. En el log aparece:
`Database created before migrations: marked as 20261112113341_InitialBaseline...`

Para Railway, antes del primer deploy con migraciones:
1. Backup (`mysqldump`).
2. Recomendado: correr `backend/scripts/2026-11-align-index-names.sql`. Las columnas ya coinciden con la
   baseline, pero algunos índices y claves foráneas tienen nombres en español; si no se renombran, una
   migración futura que toque uno de ellos fallaría.

### Cómo agregar un cambio de base de datos

1. Cambiar el modelo (`Models/`, `AppDbContext`).
2. Desde `backend/TurneraJardin.Api`:
   ```bash
   dotnet tool install --global dotnet-ef --version 9.0.*   # una sola vez
   dotnet ef migrations add NombreDelCambio
   ```
3. Revisar el archivo generado en `Migrations/` (que haga solo lo esperado).
4. Commit, PR, merge. Railway la aplica al arrancar.

Reglas:
- Nunca editar ni borrar una migración que ya llegó a producción: se corrige con otra migración.
- Si dos ramas agregan migraciones, la segunda en mergear la regenera sobre `main` (el snapshot choca).
- Backup antes de cada deploy con migraciones: en MySQL los cambios de esquema no son transaccionales.
- Ver el SQL sin ejecutarlo: `dotnet ef migrations script <desde> <hasta>`.
- EF 9 no deja arrancar la API si el modelo cambió y falta la migración (`PendingModelChangesWarning`).

## 2. Formato de errores de la API (H20)

Todas las respuestas de error usan el mismo formato, ProblemDetails (RFC 9457, `application/problem+json`):

```json
{
  "type": "urn:turnera-jardin:error:APPOINTMENT_TAKEN",
  "title": "Este turno ya no está disponible. Elegí otro horario.",
  "status": 409,
  "code": "APPOINTMENT_TAKEN",
  "message": "Este turno ya no está disponible. Elegí otro horario.",
  "requestId": "3f9c2a7d1b4e5f60"
}
```

- `message`: texto para mostrar (el frontend lo usa tal cual).
- `code`: para reaccionar a un error puntual sin depender del texto. **No se renombran**; si hace falta, se agrega uno nuevo.
- `requestId`: el mismo valor del encabezado `X-Request-Id` y de los logs de ese pedido.
- Errores de validación (400): además traen `errors` con cada campo.

| Código | Status | Cuándo |
|---|---|---|
| `VALIDATION_ERROR` | 400 | Datos inválidos en el formulario |
| `RATE_LIMITED` | 429 | Demasiados pedidos seguidos (trae `Retry-After`) |
| `CONCURRENCY_CONFLICT` | 409 | Otra persona cambió el mismo turno en ese momento |
| `DUPLICATE_KEY` | 409 | Ya existe un registro con esos datos |
| `INTERNAL_ERROR` | 500 | Error inesperado |
| `FORBIDDEN` | 403 | Turno de otra docente / sin permiso |
| `INVALID_CREDENTIALS` | 401 | Email o contraseña incorrectos |
| `ACCOUNT_LOCKED` | 429 | 5 intentos fallidos: cuenta bloqueada 15 minutos |
| `SESSION_EXPIRED` | 401 | La sesión ya no es válida |
| `PASSWORD_CHANGE_REQUIRED` | 403 | Contraseña temporal: solo se puede cambiar la contraseña |
| `WRONG_CURRENT_PASSWORD` | 400 | Contraseña actual incorrecta |
| `USER_NOT_FOUND` / `USER_EMAIL_IN_USE` | 404 / 409 | Usuarios |
| `TEACHER_HAS_USER` | 409 | La docente ya tiene usuario |
| `LAST_ADMIN` | 400 | No se puede desactivar el último usuario de dirección |
| `INVALID_IMAGE` | 400 | Foto de perfil inválida, dañada o demasiado grande |
| `TEACHER_NOT_FOUND` / `TEACHER_INACTIVE` / `TEACHER_EMAIL_IN_USE` | 404 / 400 / 409 | Docentes |
| `INVALID_DATE_RANGE` / `DATE_RANGE_TOO_LONG` / `INVALID_TIME_RANGE` | 400 | Rangos de fecha u hora |
| `INVALID_LOCATION` / `TOO_MANY_APPOINTMENTS` | 400 | Generar turnos |
| `APPOINTMENT_NOT_FOUND` | 404 | El turno no existe o ya no se puede reservar |
| `APPOINTMENT_TAKEN` | 409 | Otra familia reservó el turno |
| `APPOINTMENT_NOT_BOOKED` / `APPOINTMENT_ALREADY_BOOKED` | 409 / 400 | Acción que no corresponde al estado del turno |
| `BOOKING_TOO_SOON` / `BOOKING_TOO_FAR` | 400 | Reserva para hoy o a más de 180 días |
| `MODE_NOT_ALLOWED` | 400 | Ya no se usa (desde 2026-11-18 el ciclo no limita la modalidad) |
| `PHONE_REQUIRED` / `BOOKING_NOT_FOUND` / `CANCEL_TOO_LATE` | 400 / 404 / 400 | Cancelación desde la web |

La lista está en `Services/ApiErrors.cs` (`ErrorCodes`).

## 3. Logs y correlación (H23)

- Cada pedido tiene un id (`X-Request-Id`, se reutiliza si lo manda el cliente). Va en el encabezado de la
  respuesta, en el `requestId` de los errores y en **todas** las líneas de log de ese pedido.
- En Railway los logs salen en JSON (un objeto por línea, con `Scopes` → `RequestId`). Para encontrar un error
  que reporta alguien: buscar el código que le mostró la pantalla ("código 3f9c2a7d1b4e5f60").
- Una línea por pedido: método, ruta (sin query string), status y duración. `/health` no se registra.
- Localmente (Development) los logs siguen en texto.

## 4. Otros cambios

- **H30/H10:** usuarios creados o con contraseña restablecida, y el admin inicial, quedan con `MustChangePassword`.
  Hasta que la cambien, la API responde `PASSWORD_CHANGE_REQUIRED` en todo salvo `change-password`, y el panel
  los lleva a "Mi cuenta". El listado de Usuarios muestra "Contraseña temporal".
- **H21:** tabla `AppointmentEvents` y pantalla **Historial**: reservas, cancelaciones (familia o panel, con quién
  la hizo) y pedidos de confirmación. Guarda nombre y teléfono de la familia, no el email. Los estados `Cancelled`
  y `Completed` siguen existiendo en el enum pero no se usan (el turno cancelado vuelve a Disponible).
  *Retención:* no se borra automáticamente; si el colegio define un plazo, se puede limpiar con
  `DELETE FROM AppointmentEvents WHERE Date < '...'`.
- **H06:** el listado de Turnos y el Historial van paginados (500 por página; la API acepta `page` y `pageSize`
  hasta 2000; totales en `X-Total-Count` y `X-Booked-Count`). Los indicadores cubren todo el filtro; imprimir
  y "Eliminar disponibles" actúan sobre la página que se ve. `GET /api/teachers` y los turnos libres de una docente
  se cachean unos segundos y se invalidan en cada reserva, cancelación, generación o borrado.
- **H31:** `GET /api/appointments/{id}` (un turno libre): el formulario de reserva ya no descarga todos los turnos
  de la docente. La ventana de fechas (mañana a 180 días) la decide el servidor; el frontend no manda fechas.
- **H12:** las fotos de perfil se reconstruyen sin metadatos (EXIF con GPS, XMP, comentarios), sin datos agregados
  al final del archivo y con un máximo de 4096 × 4096 px (`Services/ImageSanitizer.cs`). No se re-codifican los
  píxeles: para eso haría falta una librería de imágenes (por ejemplo SixLabors.ImageSharp).
