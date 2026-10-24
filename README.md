# 🧸 Turnera Web — Jardín Maternal

Sistema de reserva de turnos de entrevista para jardines maternales. Los padres sacan su turno con el/la docente de su hijo/a desde un link de WhatsApp, sin llamadas ni planillas — y reciben la confirmación y un recordatorio automático por WhatsApp.

Desarrollado como proyecto académico y **cedido sin cargo** a la institución, como alternativa gratuita a sistemas pagos tipo Agenda Pro.

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)
![Angular](https://img.shields.io/badge/Angular-19-DD0031?logo=angular&logoColor=white)
![SQLite](https://img.shields.io/badge/SQLite-DB-003B57?logo=sqlite&logoColor=white)
![WhatsApp Cloud API](https://img.shields.io/badge/WhatsApp-Cloud%20API-25D366?logo=whatsapp&logoColor=white)
![License](https://img.shields.io/badge/uso-institucional%20sin%20cargo-blue)

---

## ✨ Qué resuelve

- **Familias**: eligen docente, ven los horarios libres y reservan su entrevista en un par de clics, desde el celular.
- **Institución**: gestiona a los docentes y abre turnos disponibles para cada uno desde un panel simple, sin depender de planillas compartidas.
- **Sin recordar nada a mano**: el sistema manda solo la confirmación al reservar y un recordatorio el día anterior, por WhatsApp.

## 🚀 Funcionalidades

**Flujo público (familias)**
- Selección de docente
- Calendario de horarios disponibles
- Reserva con confirmación instantánea por WhatsApp
- Cancelación de turno autogestionada

**Panel de administración**
- Login con roles (dirección / docente)
- ABM de docentes
- Login individual por docente (creado por dirección) para que cada uno vea solo sus propios turnos
- Restablecimiento de contraseñas desde el panel, sin tocar la base de datos
- Generación masiva de turnos disponibles (por rango de fechas, días y horario)
- Listado y filtrado de reservas, cancelación de turnos

**Automatizaciones**
- WhatsApp de confirmación al reservar
- WhatsApp de recordatorio un día antes de la entrevista

## 🛠️ Stack técnico

| Capa | Tecnología |
|---|---|
| Backend | C# / ASP.NET Core 8 Web API |
| ORM / Base de datos | Entity Framework Core 8 + SQLite |
| Autenticación | JWT |
| Frontend | Angular 19 (standalone components) |
| Mensajería | WhatsApp Cloud API (Meta) |

Elegido para que la institución no tenga que pagar ni instalar nada extra: SQLite es un solo archivo, y todo el stack corre en infraestructura gratuita o ya existente.

## 📦 Estructura del repositorio

```
turnera-jardin/
├── backend/     → API en ASP.NET Core (C#)
└── frontend/    → Aplicación Angular
```



## 👥 Equipo

Proyecto desarrollado por un grupo de 5 estudiantes de Desarrollo de Software del Instituto IDRA (Mar del Plata, Argentina), en el marco de un trabajo práctico ofrecido a la institución.

## 📄 Uso

Este proyecto se entrega sin cargo a la institución para su uso interno. No está pensado como producto comercial.
