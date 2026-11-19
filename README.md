# Turnera Web — Jardín Maternal

Sistema de reserva de turnos de entrevista para el Nivel Inicial del Colegio IDRA (Mar del Plata). Las familias sacan su turno con la docente de su hijo/a desde la web, sin llamadas ni planillas, y reciben la confirmación por email. Las docentes y la dirección gestionan la agenda desde un panel propio.

Desarrollado como proyecto académico y **cedido sin cargo** a la institución, como alternativa a sistemas pagos tipo Agenda Pro.

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)
![Angular 22](https://img.shields.io/badge/Angular-22-DD0031?logo=angular&logoColor=white)
![MySQL 8](https://img.shields.io/badge/MySQL-8-4479A1?logo=mysql&logoColor=white)
![Brevo](https://img.shields.io/badge/Email-Brevo-0B996E)
![License](https://img.shields.io/badge/uso-institucional%20sin%20cargo-blue)

**Reserva de turnos:** https://turnera-jardin-azure.vercel.app

---

## Qué resuelve

- **Familias**: eligen docente, ven en un calendario los días con horarios libres y reservan en tres pasos desde el celular. Pueden cancelar solas con el número de turno.
- **Docentes**: ven solo sus entrevistas, le piden confirmación a cada familia por WhatsApp con un clic e imprimen el listado del día.
- **Dirección**: carga docentes, abre turnos en bloque para una o todas las docentes y administra los accesos al panel.

## Funcionalidades

**Reserva pública (sin login)**

- Buscador de docentes por nombre o sala.
- Calendario mensual con los días que tienen lugar, horarios separados en mañana y tarde.
- Reservas desde el día siguiente y con hasta 6 meses de anticipación.
- Modalidad: la primera entrevista del niño/a en el jardín es presencial y las siguientes son virtuales (la familia lo indica al reservar; igual para todas las docentes).
- Email de confirmación con el número de turno y botón para agendarlo en Google Calendar.
- Cancelación autogestionada con número de turno + teléfono.
- Contacto por WhatsApp con el jardín (un número por turno: mañana y tarde).

**Panel (con login)**

- Tres roles: Administrador, Coordinador (dirección) y Docente; cada docente ve solo sus turnos.
- Listado de turnos con filtros, indicadores y estado del email enviado.
- Botón **Confirmación**: abre WhatsApp con un mensaje ya escrito para la familia y registra cuándo se pidió.
- Listado imprimible (A4) de las entrevistas reservadas, con columna para anotar asistencia.
- Cancelación de turnos desde el panel con aviso por email a la familia.
- Generación masiva de turnos por período, días de la semana y una o más franjas horarias, para una docente o para todas.
- Borrado en bloque de las franjas disponibles del listado (los turnos reservados se cancelan uno por uno, avisando a la familia).
- Alta, edición y desactivación de docentes.
- Gestión de usuarios: crear accesos de docentes, restablecer contraseñas, activar/desactivar.
- Cambio de contraseña propia y foto de perfil.

**Seguridad**

- JWT con roles, contraseñas con BCrypt.
- Límite de pedidos por IP en reserva, cancelación y login.
- Secretos fuera del repositorio (variables de entorno en el servidor).

## Stack técnico

| Capa                | Tecnología                                                              |
| ------------------- | ----------------------------------------------------------------------- |
| Backend             | C# / ASP.NET Core 8 Web API                                             |
| ORM / Base de datos | Entity Framework Core + MySQL 8 (Pomelo)                                |
| Autenticación       | JWT + BCrypt                                                            |
| Frontend            | Angular 22 (standalone components, signals, Reactive Forms), CSS propio |
| Emails              | Brevo (API HTTP)                                                        |
| Hosting             | Backend y base en Railway, frontend en Vercel                           |

## Estructura del repositorio

```
turnera-jardin/
├── backend/
│   ├── TurneraJardin.Api/   → API en ASP.NET Core (incluye el Dockerfile del deploy)
│   └── scripts/             → scripts SQL de mantenimiento
├── frontend/                → aplicación Angular (deploy en Vercel)
└── docs/                    → documentación interna del equipo
```

## Documentación

| Documento                                              | Para qué                                                                         |
| ------------------------------------------------------ | -------------------------------------------------------------------------------- |
| [`docs/README-interno.md`](docs/README-interno.md)     | Cómo correrlo localmente, cómo está desplegado, variables de entorno y operación |
| [`docs/roles-y-permisos.md`](docs/roles-y-permisos.md) | Qué puede hacer cada rol y cómo está implementado                                |
| [`docs/guia-swagger.md`](docs/guia-swagger.md)         | Probar la API a mano con Swagger                                                 |
| [`docs/RULES.md`](docs/RULES.md)                       | Convenciones de código                                                           |

Los manuales de uso (para familias y para el personal del jardín) se comparten aparte con la institución.

## Inicio rápido (desarrollo)

Requisitos: .NET SDK 8, Node.js 24 y MySQL 8 local.

```bash
# Backend (necesita appsettings.Development.json, ver docs/README-interno.md)
cd backend/TurneraJardin.Api
dotnet run            # http://localhost:5275 (Swagger en /swagger)

# Frontend
cd frontend
npm install
npm start             # http://localhost:4200
```

## Equipo

Proyecto desarrollado por estudiantes de Desarrollo de Software del Instituto IDRA (Mar del Plata, Argentina), en el marco de un trabajo práctico ofrecido a la institución.

## Uso

Este proyecto se entrega sin cargo a la institución para su uso interno. No está pensado como producto comercial.
