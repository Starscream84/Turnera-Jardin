-- Índices agregados por las correcciones del informe de QA (2026-11-05).
-- La base de Railway se creó con EnsureCreated(), que no modifica tablas existentes:
-- estos índices hay que crearlos a mano UNA vez (railway connect MySQL y pegar el script).
-- Antes, backup: mysqldump vía `railway connect MySQL --tunnel-only`.
-- No hay columnas nuevas: el deploy funciona aunque este script no se haya corrido todavía
-- (solo sería más lento y no tendría la protección de un usuario por docente).

-- 1) Verificar que ninguna docente tenga dos usuarios. Si esto devuelve filas,
--    desactivar/borrar el usuario sobrante antes del paso 3.
SELECT TeacherId, COUNT(*) AS usuarios
FROM Users
WHERE TeacherId IS NOT NULL
GROUP BY TeacherId
HAVING COUNT(*) > 1;

-- 2) Turnos libres de una docente (reserva pública) y listados por estado y fecha (panel, reintento de emails).
CREATE INDEX IX_Appointments_TeacherId_Status_Date ON Appointments (TeacherId, Status, Date);
CREATE INDEX IX_Appointments_Status_Date ON Appointments (Status, Date);

-- 3) Como máximo un usuario por docente (MySQL admite varios NULL: los usuarios de dirección no se ven afectados).
CREATE UNIQUE INDEX IX_Users_TeacherId_Unique ON Users (TeacherId);

-- 4) Comprobación
SHOW INDEX FROM Appointments;
SHOW INDEX FROM Users;
