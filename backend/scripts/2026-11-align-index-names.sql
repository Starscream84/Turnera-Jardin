-- Alinea los nombres de índices y claves foráneas con los que genera EF Core
-- (quedaron en español desde antes de traducir el código). No toca columnas ni datos.
-- Correr UNA vez en Railway, idealmente antes del primer deploy con migraciones
-- (ver docs/migraciones-y-errores.md). Probado el 2026-11-12 sobre una copia del esquema de producción:
-- después del script, el esquema queda idéntico al de la migración baseline.

-- Teachers
ALTER TABLE `Teachers` RENAME INDEX `IX_Docentes_Email` TO `IX_Teachers_Email`;

-- Users
ALTER TABLE `Users` RENAME INDEX `IX_Usuarios_Email` TO `IX_Users_Email`;
ALTER TABLE `Users` DROP FOREIGN KEY `FK_Usuarios_Docentes_DocenteId`;
ALTER TABLE `Users` DROP INDEX `IX_Usuarios_DocenteId`;   -- sobra: IX_Users_TeacherId_Unique ya cubre la columna
ALTER TABLE `Users` ADD CONSTRAINT `FK_Users_Teachers_TeacherId`
  FOREIGN KEY (`TeacherId`) REFERENCES `Teachers` (`Id`) ON DELETE SET NULL;

-- Appointments
ALTER TABLE `Appointments` RENAME INDEX `IX_Turnos_DocenteId_Fecha_HoraInicio` TO `IX_Appointments_TeacherId_Date_StartTime`;
ALTER TABLE `Appointments` DROP FOREIGN KEY `FK_Turnos_Docentes_DocenteId`;
ALTER TABLE `Appointments` ADD CONSTRAINT `FK_Appointments_Teachers_TeacherId`
  FOREIGN KEY (`TeacherId`) REFERENCES `Teachers` (`Id`) ON DELETE CASCADE;
