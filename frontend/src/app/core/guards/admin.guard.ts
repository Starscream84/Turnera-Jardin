import { inject } from "@angular/core";
import { CanActivateFn, Router } from "@angular/router";
import { UserRole } from "../models/auth.model";
import { AuthService } from "../services/auth.service";

function createRoleGuard(allowedRoles: UserRole[]): CanActivateFn {
    return () => {
        const auth = inject(AuthService);
        const router = inject(Router);

        const role = auth.role();
        if (role !== null && allowedRoles.includes(role)) {
            return true;
        }

        // A role without permission (e.g. a Teacher trying to enter through a direct URL)
        // is redirected to their own appointment list.
        return router.createUrlTree(["/admin/turnos"]);
    };
}

/** Requires the Admin role only — user and password management. */
export const adminGuard: CanActivateFn = createRoleGuard(["Admin"]);

/** Requires the Admin or Coordinator role — teacher and appointment management. */
export const coordinatorGuard: CanActivateFn = createRoleGuard(["Admin", "Coordinator"]);
