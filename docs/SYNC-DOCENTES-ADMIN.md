# Sincronización: feature/backend-slots-turnos + feature/docentes-admin

**Estado:** ✅ Sincronización completada  
**Rama de trabajo:** `feature/sync-docentes-admin`  
**Commit:** f73a14c  
**Fecha:** 2026-10-02

---

## Resumen de cambios aplicados

### 1. Estructura de Controladores
- ✅ Renombrado `UsuariosController` → `AdminUsuariosController`
- ✅ Alineado con convención del equipo (AdminDocentesController, AdminTurnosController)
- ✅ Removida clase `Politicas` interna de controllers
- ✅ Creado archivo dedicado `Auth/Politicas.cs` para constantes de autorización

### 2. Seguridad Avanzada (Preservada)

#### Roles
- ✅ `SuperAdmin` (equipo técnico) - acceso total
- ✅ `Admin` (directora + vicedirectora) - gestión de docentes
- ✅ `Docente` (18 cuentas) - vista y gestión de propios turnos

#### Gestión de Contraseñas
- ✅ Contraseña temporal aleatoria (12 caracteres) al crear cuenta
- ✅ Expira en 7 días (`PasswordTemporalVenceUtc`)
- ✅ Requiere cambio en primer ingreso (`DebeCambiarPassword`)
- ✅ Hash BCrypt para almacenamiento seguro

#### Sesiones
- ✅ JWT de 8 horas
- ✅ `VersionSesion` para invalidación inmediata:
  - Suspender cuenta → incrementa versión → sesión se corta en siguiente request
  - Resetear contraseña → incrementa versión → usuario debe autenticarse de nuevo

#### Auditoría
- ✅ Modelo `AuditoriaLog` con trazabilidad completa
- ✅ Registro automático de eventos sensibles

### 3. Próximos Pasos Recomendados

```bash
# Opción A: Merge local
git checkout feature/backend-slots-turnos
git merge feature/sync-docentes-admin

# Opción B: Push y crear PR
git push origin feature/sync-docentes-admin
```

**Los cambios están listos para integración segura con feature/docentes-admin**
