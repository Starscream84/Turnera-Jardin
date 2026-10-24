# Roles y permisos del panel de administración

El sistema tiene tres roles pensados para tres tipos de persona reales: el programador/mantenedor del sistema, la dirección del jardín, y cada docente.

## Índice

1. [Resumen: quién es cada rol en la vida real](#resumen-quién-es-cada-rol-en-la-vida-real)
2. [Matriz de permisos](#matriz-de-permisos)
3. [Cómo está implementado](#cómo-está-implementado)
4. [Cómo crear un usuario de cada tipo](#cómo-crear-un-usuario-de-cada-tipo)
5. [Agregar un rol nuevo en el futuro](#agregar-un-rol-nuevo-en-el-futuro)

---

## Resumen: quién es cada rol en la vida real

### `Admin` — el programador/mantenedor del sistema

Es el super usuario técnico. Tiene acceso total, incluyendo todo lo que puede hacer un `Coordinator`, más la gestión de cuentas de usuario (crear logins, restablecer contraseñas, activar/desactivar accesos). En la práctica, es quien da de alta la cuenta de dirección al principio, y a quien recurre el jardín si alguien pierde su contraseña o hace falta un cambio que no está expuesto en el panel.

### `Coordinator` — la dirección o vicedirección del jardín

Administra el funcionamiento diario sin necesitar ayuda técnica: gestiona los docentes, genera los turnos disponibles, ve la agenda completa de todo el jardín y puede cancelar cualquier turno reservado. No puede crear ni gestionar usuarios ni contraseñas — eso lo maneja el `Admin`.

### `Teacher` — cada docente del jardín

El acceso más acotado. Cada docente entra con su propio login y solo ve y puede cancelar sus propios turnos. No tiene visibilidad de la agenda de otros docentes, ni acceso a gestión de docentes, generación de turnos, o usuarios.

## Matriz de permisos

| Acción | Admin | Coordinator | Teacher |
|---|:---:|:---:|:---:|
| Ver/crear/editar docentes | ✅ | ✅ | ❌ |
| Activar/desactivar docentes | ✅ | ✅ | ❌ |
| Generar turnos nuevos | ✅ | ✅ | ❌ |
| Ver todos los turnos del jardín | ✅ | ✅ | ❌ (solo los propios) |
| Cancelar cualquier turno reservado | ✅ | ✅ | ❌ (solo los propios) |
| Eliminar una franja "Disponible" sin reservar | ✅ | ✅ | Solo las propias |
| Crear logins de usuario (Coordinador/Docente/Admin) | ✅ | ❌ | ❌ |
| Restablecer contraseñas ajenas | ✅ | ❌ | ❌ |
| Activar/desactivar accesos de usuario | ✅ | ❌ | ❌ |
| Cambiar su propia contraseña | ✅ | ✅ | ✅ |

## Cómo está implementado

### Backend

El rol vive en el enum `UserRole` (`backend/TurneraJardin.Api/Models/Enums/UserRole.cs`). En pantalla se muestran como "Administrador", "Coordinador" y "Docente":

```csharp
public enum UserRole
{
    Admin = 0,
    Teacher = 1,
    Coordinator = 2
}
```

> ⚠️ Un valor nuevo siempre va **al final**, nunca insertado en el medio. La base guarda el rol como el número, así que reordenar el enum cambiaría en silencio el rol de usuarios ya cargados.

Cada usuario logueado lleva el rol como claim en su JWT, y los controllers lo verifican de dos formas distintas según el caso:

- **Por atributo, cuando el permiso es fijo**: `AdminTeachersController` tiene `[Authorize(Roles = $"{nameof(UserRole.Admin)},{nameof(UserRole.Coordinator)}")]` a nivel de clase — cualquiera de esos dos roles entra, cualquier otro rol (o sin login) recibe `403`. `AdminUsersController` usa `[Authorize(Roles = nameof(UserRole.Admin))]`, exclusivo.
- **Por descarte, cuando depende de "es dueño o no"**: `AdminAppointmentsController` solo tiene `[Authorize]` (cualquier usuario logueado) a nivel de clase, y dentro de cada acción pregunta `IsTeacher()`: si el usuario **es** `Teacher`, lo restringe a sus propios turnos (por `teacherId`, que viaja como claim en el JWT); si **no** lo es (o sea, `Admin` o `Coordinator`), lo deja pasar sin restricción. Esto significa que agregar un rol nuevo con acceso total a turnos no requiere tocar ese controller — el diseño ya lo soporta por descarte.

`Program.cs` tiene configurado `JsonStringEnumConverter`, así que el rol viaja como texto (`"Coordinator"`) en el JSON de la API, no como número.

### Frontend

- **Tipo**: `UserRole` está definido en `frontend/src/app/core/models/auth.model.ts` como `'Admin' | 'Teacher' | 'Coordinator'`. `AuthService.role` (un `computed`) infiere este tipo automáticamente.
- **Guards**: `frontend/src/app/core/guards/admin.guard.ts` expone `adminGuard` (solo `Admin`) y `coordinatorGuard` (`Admin` o `Coordinator`), ambos generados por una función `createRoleGuard(roles)`.
- **Rutas**: en `app.routes.ts`, las rutas de docentes y de generar turnos usan `coordinatorGuard`; la de usuarios usa `adminGuard`; la de turnos no tiene guard de rol (cualquier usuario logueado entra, el backend filtra qué ve).
- **Nav**: `admin-layout.component.html` muestra "Docentes" y "Generar turnos" para `Admin`/`Coordinator`, y "Usuarios" solo para `Admin`.
- **Lista de turnos**: `appointments-list.component.html` (con `isManagement()`) muestra la columna y el filtro de "Docente" solo para `Admin`/`Coordinator` (un `Teacher` no lo necesita, porque de por sí solo ve los suyos).

## Cómo crear un usuario de cada tipo

Todo se hace desde `POST /api/admin/users` (ver también [guia-swagger.md](./guia-swagger.md)), logueado como `Admin`:

**Un Docente** (vinculado a un docente ya cargado en "Docentes"):
```json
{
  "teacherId": 3,
  "email": null,
  "fullName": null,
  "password": null,
  "role": null
}
```
Con `teacherId` presente, el backend ignora `role` (siempre crea `Teacher`) y toma el email/nombre del docente si no se especifican.

**Un Coordinador** (dirección/vicedirección):
```json
{
  "teacherId": null,
  "email": "vicedireccion@jardin.edu.ar",
  "fullName": "Nombre Apellido",
  "password": null,
  "role": "Coordinator"
}
```

**Otro Admin** (por ejemplo, otro programador del equipo):
```json
{
  "teacherId": null,
  "email": "otro-admin@jardin.edu.ar",
  "fullName": "Nombre Apellido",
  "password": null,
  "role": "Admin"
}
```
(`"role": null` también crea `Admin`, por compatibilidad con el comportamiento original del sistema.)

En los tres casos, si `password` es `null`, el backend genera una contraseña temporal aleatoria y la devuelve una única vez en la respuesta — hay que copiarla y pasársela a la persona por un medio seguro para que la cambie desde **Panel → Mi contraseña**.

## Agregar un rol nuevo en el futuro

Si más adelante hace falta un cuarto rol, el checklist es:

1. Agregar el valor al final de `UserRole` (nunca en el medio).
2. Revisar cada `[Authorize(Roles = ...)]` del backend y decidir si el rol nuevo entra ahí.
3. Revisar `AdminAppointmentsController.IsTeacher()` — si el rol nuevo debería tener acceso total a turnos (como `Admin`/`Coordinator`), no hace falta tocarlo; si debería tener un acceso restringido distinto al de `Teacher`, hay que agregar lógica nueva.
4. Ampliar el tipo `UserRole` en `auth.model.ts`.
5. Crear (o ampliar) el guard correspondiente en `admin.guard.ts`.
6. Revisar `app.routes.ts`, `admin-layout.component.html` y cualquier `.html` que compare contra un rol específico (buscar `auth.role() ===` e `isManagement` en todo el frontend) para decidir si el rol nuevo debería entrar en cada condición.
