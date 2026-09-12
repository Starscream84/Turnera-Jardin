# Flujo de trabajo en Git — equipo de 6

Guía para que cada integrante trabaje en su propia rama sin pisarse con el resto. `main` siempre tiene que quedar en un estado que funcione — nadie pushea directo ahí.

## Ramas

```
main                        ← siempre estable, es lo que se le entrega al jardín
 ├── feature/docentes-admin      (Integrante 1)
 ├── feature/turnos-backend      (Integrante 2)
 ├── feature/whatsapp            (Integrante 3)
 ├── feature/reserva-publica     (Integrante 4)
 ├── feature/panel-admin         (Integrante 5)
 └── feature/qa-deploy           (Integrante 6)
```

**Convención de nombres**: `feature/<módulo-en-el-que-trabajás>`. Evitá ramas con tu nombre propio (`juan`, `maria`) — a la semana nadie se acuerda qué tiene adentro cada una. El nombre de la rama tiene que decir *qué* se está haciendo.

Si dentro de tu módulo vas a hacer algo puntual y chico, podés abrir una sub-rama tipo `feature/whatsapp-recordatorio-retry` y mergearla a tu rama de módulo antes de ir a `main`.

## Repartija sugerida (según lo que ya está armado)

| # | Módulo | Carpetas principales |
|---|---|---|
| 1 | Docentes y autenticación (backend) | `backend/.../Controllers/AdminDocentesController.cs`, `AuthController.cs`, `Services/JwtService.cs` |
| 2 | Turnos (backend) | `backend/.../Controllers/TurnosController.cs`, `AdminTurnosController.cs`, `DocentesController.cs` |
| 3 | WhatsApp e integraciones | `backend/.../Services/WhatsAppCloudApiService.cs`, `RecordatorioBackgroundService.cs` |
| 4 | Reserva pública (frontend) | `frontend/src/app/public-booking/` |
| 5 | Panel de administración (frontend) | `frontend/src/app/admin/` |
| 6 | QA, despliegue y documentación | pruebas manuales end-to-end, README, despliegue gratuito, coordinación de PRs |

## Paso a paso para trabajar en tu rama

```bash
# 1. Parate en main y traé lo último
git checkout main
git pull origin main

# 2. Creá tu rama a partir de main
git checkout -b feature/tu-modulo

# 3. Trabajá y commiteá seguido, en commits chicos y con mensaje claro
git add .
git commit -m "Agrega validación de teléfono al reservar turno"

# 4. Subí tu rama (la primera vez con -u para que quede vinculada)
git push -u origin feature/tu-modulo
```

De ahí en adelante, para seguir subiendo cambios alcanza con `git push`.

## Mantener tu rama al día con main

Para evitar un conflicto gigante al final, traé `main` seguido (al menos una vez por semana o antes de abrir el Pull Request):

```bash
git checkout main
git pull origin main
git checkout feature/tu-modulo
git merge main
```

Si hay conflictos, Git te los marca en los archivos afectados — resolvés a mano, y después:

```bash
git add .
git commit
```

## Pull Request y merge a `main`

1. Cuando tu parte funciona, abrí un Pull Request en GitHub de `feature/tu-modulo` hacia `main`.
2. En la descripción, contá qué hiciste y cómo probarlo (2-3 líneas alcanza).
3. Otro integrante del equipo lo revisa y aprueba antes de mergear (aunque sea una lectura rápida — el objetivo es que dos personas hayan visto el código antes de que llegue a `main`).
4. Mergeá con **"Squash and merge"** si tu rama tiene muchos commits chicos de prueba, así el historial de `main` queda prolijo.
5. Borrá la rama después de mergeada (GitHub te lo ofrece con un botón).

### Configurar la protección de `main` (lo hace quien administra el repo, una sola vez)

En GitHub: **Settings → Branches → Add branch ruleset** (o *Branch protection rule* en repos más viejos) sobre `main`:
- ✅ Require a pull request before merging
- ✅ Require approvals (mínimo 1)
- ❌ No permitir push directo a `main`

Esto evita que alguien pushee sin querer directo a `main` y rompa lo que ya funciona.

## Reglas básicas para no pisarse

- **Un módulo, una rama.** Si necesitás tocar código de otro módulo (por ejemplo, el frontend necesita un campo nuevo que agrega el backend), avisá en el grupo antes de tocarlo vos mismo.
- **Commits chicos y frecuentes** en vez de un commit gigante al final — es mucho más fácil de revisar y de resolver conflictos.
- **`main` se toca solo por Pull Request**, nunca con push directo.
- Antes de abrir el PR, corré el proyecto localmente y probá tu parte (ver el `README-interno.md` para cómo levantar el backend y el frontend).
