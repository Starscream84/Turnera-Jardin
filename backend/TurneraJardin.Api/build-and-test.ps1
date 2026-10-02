# Clean and rebuild the project
$projectRoot = "C:\Users\Usuario\Desktop\ProyectoTurnera\Turnera-Jardin\backend\TurneraJardin.Api"

Write-Host "=== Limpiando directorios de compilación ===" -ForegroundColor Green
Remove-Item -Path "$projectRoot\bin" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "$projectRoot\obj" -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "✅ Directorios limpios`n"

Write-Host "=== Compilando el proyecto ===" -ForegroundColor Green
cd $projectRoot
dotnet build

Write-Host "`n=== Compilación completada ===" -ForegroundColor Green
