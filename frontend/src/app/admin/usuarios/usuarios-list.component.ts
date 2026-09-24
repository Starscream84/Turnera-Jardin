import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { AdminUsuariosService } from '../../core/services/admin-usuarios.service';
import { AdminDocentesService } from '../../core/services/admin-docentes.service';
import { UsuarioAdmin } from '../../core/models/usuario.model';
import { DocenteAdmin } from '../../core/models/docente.model';

/**
 * Pantalla de dirección para gestionar los accesos al panel: crear el login individual
 * de cada docente (para que solo vea sus propios turnos) y restablecer contraseñas.
 */
@Component({
  selector: 'app-usuarios-list',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './usuarios-list.component.html'
})
export class UsuariosListComponent implements OnInit {
  private fb = inject(FormBuilder);
  private usuariosService = inject(AdminUsuariosService);
  private docentesService = inject(AdminDocentesService);

  usuarios = signal<UsuarioAdmin[]>([]);
  docentesSinUsuario = signal<DocenteAdmin[]>([]);
  cargando = signal(true);
  creando = signal(false);
  error = signal<string | null>(null);
  credencialesGeneradas = signal<{ email: string; passwordTemporal: string } | null>(null);

  formCrear = this.fb.nonNullable.group({
    docenteId: ['', Validators.required],
    password: ['']
  });

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    this.cargando.set(true);
    this.error.set(null);

    forkJoin({
      usuarios: this.usuariosService.listar(),
      docentes: this.docentesService.listar()
    }).subscribe({
      next: ({ usuarios, docentes }) => {
        this.usuarios.set(usuarios);
        const idsConUsuario = new Set(usuarios.map((u) => u.docenteId).filter((id): id is number => id !== null));
        this.docentesSinUsuario.set(docentes.filter((d) => d.activo && !idsConUsuario.has(d.id)));
        this.cargando.set(false);
      },
      error: () => {
        this.error.set('No pudimos cargar los usuarios.');
        this.cargando.set(false);
      }
    });
  }

  crearParaDocente(): void {
    if (this.formCrear.invalid) {
      this.formCrear.markAllAsTouched();
      return;
    }

    const valores = this.formCrear.getRawValue();
    this.creando.set(true);
    this.error.set(null);
    this.credencialesGeneradas.set(null);

    this.usuariosService
      .crear({ docenteId: Number(valores.docenteId), password: valores.password || undefined })
      .subscribe({
        next: (creado) => {
          this.creando.set(false);
          this.credencialesGeneradas.set({ email: creado.email, passwordTemporal: creado.passwordTemporal });
          this.formCrear.reset({ docenteId: '', password: '' });
          this.cargar();
        },
        error: (err) => {
          this.creando.set(false);
          this.error.set(err?.error?.mensaje ?? 'No pudimos crear el usuario.');
        }
      });
  }

  restablecer(usuario: UsuarioAdmin): void {
    const confirmado = confirm(`¿Restablecer la contraseña de ${usuario.nombreCompleto}? Se va a generar una contraseña temporal nueva.`);
    if (!confirmado) return;

    this.error.set(null);
    this.credencialesGeneradas.set(null);

    this.usuariosService.restablecerPassword(usuario.id).subscribe({
      next: (resultado) => {
        this.credencialesGeneradas.set({ email: resultado.email, passwordTemporal: resultado.passwordTemporal });
      },
      error: () => this.error.set('No pudimos restablecer la contraseña.')
    });
  }

  alternarActivo(usuario: UsuarioAdmin): void {
    const verbo = usuario.activo ? 'desactivar' : 'activar';
    const confirmado = confirm(`¿Seguro que querés ${verbo} el acceso de ${usuario.nombreCompleto}?`);
    if (!confirmado) return;

    this.error.set(null);
    const accion = usuario.activo ? this.usuariosService.desactivar(usuario.id) : this.usuariosService.activar(usuario.id);

    accion.subscribe({
      next: () => this.cargar(),
      error: (err) => this.error.set(err?.error?.mensaje ?? `No pudimos ${verbo} el acceso.`)
    });
  }

  cerrarCredenciales(): void {
    this.credencialesGeneradas.set(null);
  }
}
