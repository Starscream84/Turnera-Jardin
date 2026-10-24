-- =====================================================================
-- Turnera Jardín: rename tables and columns to English (refactor/english-codebase)
-- MySQL 8+. Run ONCE on a database created by the Spanish version of the code.
--
-- Before running:
--   1. Backup:  mysqldump ... > backup-before-english.sql
--   2. Check the current columns match (see the CHECK queries at the bottom).
--
-- MySQL DDL is not transactional: if a statement fails, fix it and continue
-- from that statement, don't re-run the ones that already succeeded.
--
-- Indexes and foreign keys keep their old names (IX_Turnos_..., FK_Turnos_Docentes_...).
-- That is harmless: EF Core does not check constraint names at runtime.
-- Enums are stored as integers and their numeric values did not change.
-- =====================================================================

-- 1) Tables (single atomic statement)
RENAME TABLE
    `Docentes` TO `Teachers`,
    `Turnos`   TO `Appointments`,
    `Usuarios` TO `Users`;

-- 2) Teachers
ALTER TABLE `Teachers`
    RENAME COLUMN `Nombre`   TO `FirstName`,
    RENAME COLUMN `Apellido` TO `LastName`,
    RENAME COLUMN `Sala`     TO `Classroom`,
    RENAME COLUMN `Ciclo`    TO `Cycle`,
    RENAME COLUMN `Activo`   TO `IsActive`;

-- 3) Appointments
ALTER TABLE `Appointments`
    RENAME COLUMN `DocenteId`              TO `TeacherId`,
    RENAME COLUMN `Fecha`                  TO `Date`,
    RENAME COLUMN `HoraInicio`             TO `StartTime`,
    RENAME COLUMN `HoraFin`                TO `EndTime`,
    RENAME COLUMN `Estado`                 TO `Status`,
    RENAME COLUMN `NombreResponsable`      TO `GuardianFirstName`,
    RENAME COLUMN `ApellidoResponsable`    TO `GuardianLastName`,
    RENAME COLUMN `TelefonoPadre`          TO `GuardianPhone`,
    RENAME COLUMN `EmailPadre`             TO `GuardianEmail`,
    RENAME COLUMN `NombreNino`             TO `ChildFirstName`,
    RENAME COLUMN `ApellidoNino`           TO `ChildLastName`,
    RENAME COLUMN `Observaciones`          TO `Notes`,
    RENAME COLUMN `Modalidad`              TO `Mode`,
    RENAME COLUMN `FechaReserva`           TO `BookedAt`,
    RENAME COLUMN `ConfirmacionEnviada`    TO `ConfirmationSent`,
    RENAME COLUMN `RecordatorioEnviado`    TO `ReminderSent`,
    RENAME COLUMN `ConfirmacionSolicitada` TO `ConfirmationRequestedAt`,
    RENAME COLUMN `FechaCreacion`          TO `CreatedAt`;

-- 4) Users
ALTER TABLE `Users`
    RENAME COLUMN `NombreCompleto` TO `FullName`,
    RENAME COLUMN `Rol`            TO `Role`,
    RENAME COLUMN `DocenteId`      TO `TeacherId`,
    RENAME COLUMN `Activo`         TO `IsActive`,
    RENAME COLUMN `FotoUrl`        TO `PhotoUrl`;

-- =====================================================================
-- CHECK: run before (with the Spanish names) and after (with the English names).
-- After the script, every table must list only English column names.
-- =====================================================================
-- SELECT TABLE_NAME, COLUMN_NAME, COLUMN_TYPE
-- FROM information_schema.COLUMNS
-- WHERE TABLE_SCHEMA = DATABASE()
-- ORDER BY TABLE_NAME, ORDINAL_POSITION;
