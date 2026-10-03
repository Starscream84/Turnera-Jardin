# Script para hacer commit final de los cambios
# Ejecutar desde la carpeta raíz del proyecto (donde está .git)
# PowerShell: .\COMMIT_FINAL.ps1

Write-Host "╔══════════════════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║               🚀 COMMIT FINAL - Cambios Backend Turnera Jardín              ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Verificar que estamos en un repo git
if (-not (Test-Path ".git")) {
    Write-Host "❌ ERROR: No estamos en un repositorio git" -ForegroundColor Red
    Write-Host "Navega a la carpeta raíz del proyecto y ejecuta este script de nuevo" -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ Repositorio git detectado" -ForegroundColor Green
Write-Host ""

# Mostrar estado actual
Write-Host "📊 Estado actual del repositorio:" -ForegroundColor Yellow
git status --short
Write-Host ""

# Preguntar confirmación
$confirmacion = Read-Host "¿Estás seguro de que deseas hacer commit de estos cambios? (s/n)"
if ($confirmacion -ne "s" -and $confirmacion -ne "S") {
    Write-Host "❌ Commit cancelado" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "📝 Preparando commit..." -ForegroundColor Yellow

# Stage todos los cambios
Write-Host "  → Staging cambios..."
git add -A

# Hacer el commit con mensaje descriptivo
Write-Host "  → Creando commit..."
$commitMessage = @"
feat: Implementar separación nombre/apellido y validación telefónica

- Agregar campos separados nombre/apellido para adultos y niños en Turno model
- Crear PhoneValidator utility para validación de teléfonos Mar del Plata (549223)
- Actualizar ReservaTurnoDto con parámetros de apellido y método de validación
- Actualizar TurnoConfirmadoDto con campo ApellidoNino
- Actualizar TurnoAdminDto con campos ApellidoPadre y ApellidoNino
- Crear ITurnosService interface para inyección de dependencias
- Mejorar TurnosService.ReservarTurnoAsync() con validación DTO
- Cambio de contrato API: frontend debe enviar nombre/apellido separados
- Requiere migración EF Core: AddParentAndChildLastNames
- Validación de teléfono: E.164 format sin '+' (549223XXXXXXXX - 14 dígitos)

BREAKING CHANGE: API contract actualizado para separar nombre y apellido
"@

git commit -m $commitMessage

# Verificar resultado
if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✅ COMMIT EXITOSO" -ForegroundColor Green
    Write-Host ""
    Write-Host "📋 Información del commit:" -ForegroundColor Cyan
    git log --oneline -1
    Write-Host ""
    Write-Host "📊 Cambios incluidos:" -ForegroundColor Yellow
    git show --stat
    Write-Host ""
    Write-Host "🚀 Próximo paso: push a feature/backend-slots-turnos" -ForegroundColor Yellow
    Write-Host "   git push origin feature/backend-slots-turnos" -ForegroundColor Cyan
} else {
    Write-Host ""
    Write-Host "❌ ERROR al hacer commit" -ForegroundColor Red
    Write-Host "Verifica el estado del repositorio e intenta de nuevo" -ForegroundColor Yellow
}
