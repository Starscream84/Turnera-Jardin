import { inject } from "@angular/core";
import { CanActivateFn, Router } from "@angular/router";
import { AuthService } from "../services/auth.service";

function crearGuardPorRol(rolesPermitidos: string[]): CanActivateFn {
    return () => {
        const auth = inject(AuthService);
        const router = inject(Router);

        const rol = auth.rol();
        if (rol !== null && rolesPermitidos.includes(rol)) {
            return true;
        }

        // Un rol sin permiso (por ejemplo, Docente intentando entrar por URL directa)
        // es redirigido a su propia lista de turnos.
        return router.createUrlTree(["/admin/turnos"]);
    };
}

/** Exige rol Admin exclusivamente — gestión de usuarios y contraseñas. */
export const adminGuard: CanActivateFn = crearGuardPorRol(["Admin"]);

/** Exige rol Admin o Coordinador — gestión de docentes y turnos. */
export const coordinadorGuard: CanActivateFn = crearGuardPorRol(["Admin", "Coordinador"]);
