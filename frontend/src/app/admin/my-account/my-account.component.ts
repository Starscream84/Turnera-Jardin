import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../core/services/auth.service';
import { initials } from '../../shared/avatar.util';

const MAX_SIZE_BYTES = 2 * 1024 * 1024; // 2 MB, must match the backend limit
const VALID_TYPES = ['image/jpeg', 'image/png', 'image/webp'];

/** Screen where any logged-in user (management or teacher) changes their profile photo and their own password. */
@Component({
  selector: 'app-my-account',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './my-account.component.html'
})
export class MyAccountComponent {
  private fb = inject(FormBuilder);
  auth = inject(AuthService);

  readonly initials = initials;

  saving = signal(false);
  error = signal<string | null>(null);
  success = signal(false);

  uploadingPhoto = signal(false);
  photoError = signal<string | null>(null);

  form = this.fb.nonNullable.group({
    currentPassword: ['', Validators.required],
    newPassword: ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', Validators.required]
  });

  changePassword(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const values = this.form.getRawValue();
    if (values.newPassword !== values.confirmPassword) {
      this.error.set('Las dos contraseñas nuevas no coinciden.');
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    this.success.set(false);

    this.auth
      .changePassword({ currentPassword: values.currentPassword, newPassword: values.newPassword })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.success.set(true);
          this.form.reset();
        },
        error: (err) => {
          this.saving.set(false);
          this.error.set(err?.error?.message ?? 'No pudimos cambiar la contraseña.');
        }
      });
  }

  selectPhoto(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.photoError.set(null);

    if (!VALID_TYPES.includes(file.type)) {
      this.photoError.set('Solo se aceptan imágenes JPG, PNG o WEBP.');
      input.value = '';
      return;
    }

    if (file.size > MAX_SIZE_BYTES) {
      this.photoError.set('La imagen no puede superar los 2 MB.');
      input.value = '';
      return;
    }

    this.uploadingPhoto.set(true);
    this.auth.uploadPhoto(file).subscribe({
      next: () => {
        this.uploadingPhoto.set(false);
        input.value = '';
      },
      error: (err) => {
        this.uploadingPhoto.set(false);
        this.photoError.set(err?.error?.message ?? 'No pudimos subir la foto.');
        input.value = '';
      }
    });
  }
}
