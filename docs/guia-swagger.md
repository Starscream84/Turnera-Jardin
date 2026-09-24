# Guía de Swagger — probar la API a mano

Swagger es la documentación interactiva que genera automáticamente ASP.NET Core a partir de los controllers. Sirve para probar cualquier endpoint sin necesitar Postman, curl, ni el frontend.

## Índice

1. [Levantar el backend y abrir Swagger](#levantar-el-backend-y-abrir-swagger)
2. [Probar un endpoint sin login](#probar-un-endpoint-sin-login)
3. [Loguearse y usar el token (Authorize)](#loguearse-y-usar-el-token-authorize)
4. [Crear un usuario Coordinador de prueba](#crear-un-usuario-coordinador-de-prueba)
5. [Errores comunes al usar Swagger](#errores-comunes-al-usar-swagger)
6. [Probar los permisos por rol](#probar-los-permisos-por-rol)

---

## Levantar el backend y abrir Swagger

```bash
cd backend/TurneraJardin.Api
dotnet run
```

Cuando la consola muestra `Now listening on: http://localhost:5275`, abrí en el navegador:

```
http://localhost:5275/swagger
```

Vas a ver todos los controllers como secciones desplegables (`Auth`, `AdminDocentes`, `AdminTurnos`, `AdminUsuarios`, etc.), y dentro de cada uno sus endpoints agrupados por método HTTP.

> Si al correr `dotnet run` te aparece `Failed to bind to address ... address already in use`, es que quedó un proceso anterior corriendo en el puerto 5275. Buscalo con `lsof -nP -iTCP:5275 -sTCP:LISTEN` en la terminal de tu Mac y matalo con `kill -9 <PID>` antes de volver a intentar.

## Probar un endpoint sin login

Para familiarizarte con la mecánica, arrancá por algo público, como `POST /api/auth/login`:

1. Hacé clic en el endpoint para expandirlo.
2. Clic en **"Try it out"**.
3. Completá el JSON de ejemplo con datos reales.
4. Clic en **"Execute"**.
5. Abajo aparece la respuesta: código de estado, body, y el `curl` equivalente.

## Loguearse y usar el token (Authorize)

La mayoría de los endpoints de `/api/admin/...` están protegidos: necesitan un JWT que identifique con qué usuario (y qué rol) estás actuando.

1. Ejecutá `POST /api/auth/login` con un usuario válido. La respuesta trae un campo `token` con el JWT.
2. Copiá **solo el valor del token**, sin las comillas que lo rodean en el JSON.
3. Arriba de la página de Swagger hay un botón **Authorize** (ícono de candado 🔓).
4. Pegá el token en el campo. Como el backend usa el esquema `Bearer`, escribí:
   ```
   Bearer <tu-token-acá>
   ```
5. Clic en **Authorize** y después en **Close**.

A partir de ahí, todos los endpoints protegidos usan ese token automáticamente en cada "Execute", hasta que refresques la página o el token expire.

## Crear un usuario Coordinador de prueba

Con el token de un usuario `Admin` cargado en Authorize:

1. Buscá `POST /api/admin/usuarios`, clic en "Try it out".
2. Reemplazá el JSON de ejemplo por:
   ```json
   {
     "docenteId": null,
     "email": "coordinador@jardin.edu.ar",
     "nombreCompleto": "Coordinador de Prueba",
     "password": null,
     "rol": "Coordinador"
   }
   ```
3. Ejecutá. La respuesta (`201 Created`) trae `passwordTemporal` — es la **única vez** que el backend te la muestra en texto plano, copiala.
4. Logueate con ese email y esa contraseña en `POST /api/auth/login` para obtener el JWT de ese usuario, y cargalo en Authorize para probar sus permisos.

## Errores comunes al usar Swagger

**"Request body" vs "Responses"**
Cada endpoint tiene dos cajas de JSON distintas: arriba, el **Request body** (editable, es lo que enviás) y abajo, en **Responses → 201 → Example Value**, un ejemplo de lo que vas a *recibir* (no se edita ni se envía). Si copiás y pegás el body sin querer desde la caja de abajo, no vas a mandar el body correcto.

**`docenteId: 0` no es lo mismo que `null`**
Swagger pone `0` como placeholder por defecto para un campo numérico opcional (`int?`). Si dejás ese `0`, el backend interpreta que querés crear el login de un docente (el que tenga `Id = 0`, que probablemente no existe) en vez de un usuario de dirección. Reemplazalo siempre por `null` a mano cuando corresponda.

**`password: ""` no es lo mismo que `password: null`**
Si tocás el campo y lo dejás en blanco, puede quedar como string vacío (`""`) en vez de `null`. La validación `[MinLength(8)]` no se dispara con `null` (lo ignora y genera una contraseña automática), pero **sí** se dispara con `""` (longitud 0 < 8), y te va a devolver un `400` con el mensaje "La contraseña debe tener al menos 8 caracteres." Fijate que el JSON diga literalmente `null`, sin comillas.

**El campo `rol` no aparece en el ejemplo**
Si acabás de agregar un campo a un DTO (`UsuarioCreateDto`, por ejemplo) y no lo ves en Swagger, es porque el proceso de `dotnet run` que tenés corriendo compiló antes de ese cambio — Swagger arma el esquema a partir de lo que está cargado en memoria, no relee el archivo `.cs` en cada request. Solución: `Ctrl+C` en la terminal del backend, y `dotnet run` de nuevo.

**Confundir el endpoint de cancelar turno "admin" con el "público"**
Hay dos formas de cancelar un turno:
- `POST /api/turnos/{id}/cancelar?telefono=` — **pública**, sin login, para que la familia cancele su propio turno validando su teléfono.
- `POST /api/admin/turnos/{id}/cancelar` — **protegida** (`Admin`/`Coordinador`, o el propio `Docente` dueño del turno), sin pedir teléfono, para el panel de administración.

Si estás probando permisos por rol, asegurate de usar la segunda. La primera funciona igual sin importar qué token tengas cargado (o sin ninguno).

## Probar los permisos por rol

Con el token de cada rol cargado en Authorize, este es el checklist para confirmar que las restricciones funcionan:

| Endpoint | Admin | Coordinador | Docente |
|---|---|---|---|
| `GET /api/admin/docentes` | 200 | 200 | 403 |
| `GET /api/admin/turnos` | 200 (todos) | 200 (todos) | 200 (solo los suyos) |
| `POST /api/admin/turnos/{id}/cancelar` | 204 | 204 | 204 (solo si es suyo, si no 403) |
| `GET /api/admin/usuarios` | 200 | 403 | 403 |
| `POST /api/admin/usuarios` | 201 | 403 | 403 |

Si algún resultado no coincide con esta tabla (por ejemplo, un Coordinador entrando a `/api/admin/usuarios` sin que le dé 403), hay que revisar la configuración de autenticación/roles antes de dar por buena la implementación.
