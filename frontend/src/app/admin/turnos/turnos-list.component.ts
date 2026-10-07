import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { AdminTurnosService, FiltroTurnosAdmin } from '../../core/services/admin-turnos.service';
import { AdminDocentesService } from '../../core/services/admin-docentes.service';
import { AuthService } from '../../core/services/auth.service';
import { TurnoAdmin, EstadoTurno } from '../../core/models/turno.model';
import { DocenteAdmin } from '../../core/models/docente.model';

@Component({
  selector: 'app-turnos-list',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './turnos-list.component.html'
})
export class TurnosListComponent implements OnInit {
  private fb = inject(FormBuilder);
  private service = inject(AdminTurnosService);
  private docentesService = inject(AdminDocentesService);
  auth = inject(AuthService);

  turnos = signal<TurnoAdmin[]>([]);
  docentes = signal<DocenteAdmin[]>([]);
  cargando = signal(true);
  error = signal<string | null>(null);
  aviso = signal<string | null>(null);
  eliminando = signal(false);

  /** Filtro con el que se cargó el listado en pantalla (no lo que esté tipeado sin buscar todavía). */
  private filtroAplicado: FiltroTurnosAdmin = {};

  /** Totales del listado que está en pantalla (según el filtro aplicado), para los indicadores de arriba. */
  resumen = computed(() => {
    const turnos = this.turnos();
    const contar = (estado: EstadoTurno) => turnos.filter((t) => t.estado === estado).length;
    return {
      reservados: contar('Reservado'),
      disponibles: contar('Disponible'),
      cancelados: contar('Cancelado'),
      asistenciaPedida: turnos.filter((t) => t.estado === 'Reservado' && t.confirmacionSolicitada).length
    };
  });

  filtro = this.fb.nonNullable.group({
    docenteId: [''],
    desde: [''],
    hasta: [''],
    estado: ['' as EstadoTurno | '']
  });

  ngOnInit(): void {
    if (this.auth.rol() === 'Admin' || this.auth.rol() === 'Coordinador') {
      this.docentesService.listar().subscribe({ next: (docentes) => this.docentes.set(docentes) });
    }
    this.buscar();
  }

  buscar(): void {
    this.cargando.set(true);
    this.error.set(null);
    const valores = this.filtro.getRawValue();
    const filtro: FiltroTurnosAdmin = {
      docenteId: valores.docenteId ? Number(valores.docenteId) : undefined,
      desde: valores.desde || undefined,
      hasta: valores.hasta || undefined,
      estado: valores.estado || undefined
    };
    this.filtroAplicado = filtro;

    this.service
      .listar(filtro)
      .subscribe({
        next: (turnos) => {
          this.turnos.set(turnos);
          this.cargando.set(false);
        },
        error: () => {
          this.error.set('No pudimos cargar los turnos.');
          this.cargando.set(false);
        }
      });
  }

  /**
   * Borra todos los turnos del listado en pantalla (los que trajo el último "Buscar").
   * Sin filtros, eso es todos los turnos del sistema.
   */
  eliminarListados(): void {
    const total = this.turnos().length;
    if (total === 0) return;

    const reservados = this.resumen().reservados;
    const advertencia = reservados
      ? `\n\nATENCIÓN: ${reservados} tienen una familia asignada. Se borran igual y NO se les avisa.`
      : '';
    const confirmado = confirm(
      `¿Eliminar los ${total} turnos de este listado?${advertencia}\n\nEsta acción no se puede deshacer.`
    );
    if (!confirmado) return;

    this.eliminando.set(true);
    this.error.set(null);
    this.aviso.set(null);

    this.service.eliminarVarios(this.filtroAplicado).subscribe({
      next: (respuesta) => {
        this.eliminando.set(false);
        this.aviso.set(`Se eliminaron ${respuesta.eliminados} turno(s).`);
        this.buscar();
      },
      error: () => {
        this.eliminando.set(false);
        this.error.set('No pudimos eliminar los turnos.');
      }
    });
  }

  cancelar(turno: TurnoAdmin): void {
    const confirmado = confirm(`¿Cancelar el turno de ${turno.nombreNino} (${turno.nombrePadre})?`);
    if (!confirmado) return;

    this.service.cancelar(turno.id).subscribe({
      next: () => this.buscar(),
      error: () => this.error.set('No pudimos cancelar el turno.')
    });
  }

  /**
   * Abre WhatsApp (app en el celular, WhatsApp Web en la compu) con el mensaje ya escrito para la
   * familia: recuerda el turno y pide que confirmen. Sale desde el WhatsApp de quien toca el botón,
   * así que no usa la API de Meta ni tiene costo; la respuesta llega a ese mismo chat.
   */
  pedirConfirmacion(turno: TurnoAdmin): void {
    const telefono = this.telefonoParaWhatsApp(turno.telefonoPadre);
    if (!telefono) {
      this.error.set('Este turno no tiene un WhatsApp válido cargado.');
      return;
    }

    // Se abre dentro del mismo click: si se abriera después de la respuesta del servidor,
    // el navegador lo bloquearía como ventana emergente.
    const url = `https://wa.me/${telefono}?text=${encodeURIComponent(this.mensajeConfirmacion(turno))}`;
    window.open(url, '_blank', 'noopener');

    this.error.set(null);
    this.service.solicitarConfirmacion(turno.id).subscribe({
      next: () => {
        const ahora = new Date().toISOString();
        this.turnos.update((turnos) =>
          turnos.map((t) => (t.id === turno.id ? { ...t, confirmacionSolicitada: ahora } : t))
        );
      },
      error: () =>
        this.error.set('Se abrió WhatsApp, pero no pudimos dejar registrado que se pidió la confirmación.')
    });
  }

  mensajeConfirmacion(turno: TurnoAdmin): string {
    const [anio, mes, dia] = turno.fecha.split('-').map(Number);
    const fecha = new Date(anio, mes - 1, dia).toLocaleDateString('es-AR', {
      weekday: 'long',
      day: 'numeric',
      month: 'long'
    });
    const saludo = turno.nombrePadre ? `Hola ${turno.nombrePadre}` : 'Hola';
    const modalidad = turno.modalidad ? ` (${turno.modalidad.toLowerCase()})` : '';

    return (
      `${saludo}, te escribimos del jardín para recordarte la entrevista por ${turno.nombreNino ?? 'tu hijo/a'} ` +
      `con ${turno.docenteNombre}: ${fecha} a las ${this.formatearHora(turno.horaInicio)} hs${modalidad}.\n\n` +
      `¿Nos confirmás si vas a poder asistir? Respondé este mensaje con SÍ o NO.\n\n` +
      `Turno N.º ${turno.id}. ¡Gracias!`
    );
  }

  /**
   * wa.me necesita el número completo con código de país y sin "+": para un celular argentino,
   * 549 + código de área + número. Las familias lo cargan como "2235551234", así que se completa acá.
   */
  telefonoParaWhatsApp(telefono: string | null): string | null {
    let digitos = (telefono ?? '').replace(/\D/g, '');
    if (digitos.startsWith('00')) digitos = digitos.slice(2);

    if (digitos.startsWith('54')) {
      digitos = digitos.slice(2);
      if (digitos.startsWith('9')) digitos = digitos.slice(1);
    }
    if (digitos.startsWith('0')) digitos = digitos.slice(1);

    if (digitos.length === 10) return `549${digitos}`;
    // Otro largo: puede ser un número del exterior, se usa tal cual vino.
    const original = (telefono ?? '').replace(/\D/g, '');
    return original.length >= 8 ? original : null;
  }

  /** "03/10 08:40" en hora local. El backend guarda en UTC y lo devuelve sin la "Z". */
  formatearFechaHora(iso: string): string {
    const fecha = new Date(/Z|[+-]\d\d:\d\d$/.test(iso) ? iso : `${iso}Z`);
    return fecha.toLocaleString('es-AR', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' });
  }

  eliminar(turno: TurnoAdmin): void {
    const confirmado = confirm('¿Eliminar esta franja horaria disponible?');
    if (!confirmado) return;

    this.service.eliminar(turno.id).subscribe({
      next: () => this.buscar(),
      error: () => this.error.set('No pudimos eliminar el turno.')
    });
  }

  formatearFecha(fecha: string): string {
    const [anio, mes, dia] = fecha.split('-').map(Number);
    return new Date(anio, mes - 1, dia).toLocaleDateString('es-AR');
  }

  formatearHora(hora: string): string {
    return hora.slice(0, 5);
  }

  claseBadge(estado: EstadoTurno): string {
    return {
      Disponible: 'badge-disponible',
      Reservado: 'badge-reservado',
      Cancelado: 'badge-cancelado',
      Completado: 'badge-completado'
    }[estado];
  }
}
