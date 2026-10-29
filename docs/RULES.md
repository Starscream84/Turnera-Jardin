# Convenciones de código — Turnera Jardín

Guía de estilo para el equipo: backend en C#/ASP.NET Core, frontend en Angular.

## 1. Comentarios

**Principio general**: comentar el *por qué*, no el *qué*. 

- ❌ Evitar comentarios redundantes:


- ✅ Comentar decisiones o trade-offs:
```csharp

**Documentación de API pública (backend)**: usar XML doc comments (`///`) en controllers, servicios públicos y DTOs — Swagger los levanta automáticamente.
```csharp
/// <summary>
/// Reserva un turno disponible y dispara la confirmación por WhatsApp.
/// </summary>
/// <exception cref="AppointmentNotAvailableException">Si el turno ya fue tomado.</exception>
public async Task<Appointment> BookAppointmentAsync(int appointmentId, string parentPhone)
```

**Frontend (Angular/TS)**: JSDoc solo en servicios compartidos (`@Injectable`) y funciones con lógica no trivial. No hace falta documentar componentes triviales de presentación.

**TODOs**: formato fijo para poder buscarlos con grep, con nombre y fecha:
```
// TODO(matias, 2026-09): implementar reintento de WhatsApp fallido
```

**Idioma**: Inglés.


## 2. Nomenclatura de variables y símbolos

### Backend (C# / ASP.NET Core)

| Elemento | Convención | Ejemplo |
|---|---|---|
| Clases, records, enums | PascalCase | `AppointmentService`, `AppointmentStatus` |
| Interfaces | `I` + PascalCase | `IAppointmentRepository` |
| Métodos, propiedades públicas | PascalCase | `BookAppointmentAsync` |
| Parámetros, variables locales | camelCase | `appointmentId`, `bookingDate` |
| Campos privados | `_camelCase` | `_context`, `_whatsappClient` |
| Constantes | PascalCase (no ALL_CAPS en C#) | `const int MaxTeachers = 18;` |
| Métodos async | sufijo `Async` | `SendReminderAsync` |
| Booleanos | prefijo `is`/`has` | `isAvailable`, `hasConfirmed` |

### Frontend (Angular / TypeScript)

| Elemento | Convención | Ejemplo |
|---|---|---|
| Clases, componentes, interfaces | PascalCase | `AppointmentListComponent`, `Appointment` |
| Archivos | kebab-case | `appointment-list.component.ts` |
| Selectores de componente | kebab-case con prefijo del proyecto | `app-appointment-list` |
| Variables, funciones, métodos | camelCase | `availableAppointments`, `bookAppointment()` |
| Constantes globales | UPPER_SNAKE_CASE | `WHATSAPP_API_URL` |
| Observables (RxJS clásico) | sufijo `$` (no hace falta con signals) | `appointments$` |
| Booleanos | prefijo `is`/`has`/`can` | `isLoading`, `canEdit` |

### Reglas generales para ambos lados
- Nombres descriptivos, sin abreviaturas crípticas (`teacherId`, no `teachId` ni `tid`).
- Evitar nombres genéricos (`data`, `info`, `temp`, `obj`) salvo en scopes muy acotados.
- El dominio del jardín se nombra en inglés: `appointment`, `teacher`, `parent`, `timeSlot` — no términos genéricos como `record`, `entity`, `slot`.
