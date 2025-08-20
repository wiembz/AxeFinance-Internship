import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-superadmin-signup',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatButtonModule, RouterLink],
  templateUrl: './superadmin-signup.component.html',
  styleUrls: ['./superadmin-signup.component.scss']
})
export class SuperadminSignupComponent {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);
  private snackBar = inject(MatSnackBar);
  loading = false;
  apiError: string | null = null;
  apiSuccess: string | null = null;

  form = this.fb.group({
    username: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [
      Validators.required,
      Validators.minLength(8),
      Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]+$/)
    ]]
  });

  get username() { return this.form.get('username'); }
  get email() { return this.form.get('email'); }
  get password() { return this.form.get('password'); }

  onSubmit() {
    if (this.form.invalid) return;
    this.loading = true;
    this.apiError = null;
    this.apiSuccess = null;
    this.auth.register(this.form.value as any).subscribe({
      next: res => {
        this.loading = false;
        if (res.success) {
          this.apiSuccess = 'Superadmin account created successfully!';
          this.snackBar.open(this.apiSuccess, 'OK', { duration: 3000 });
          this.form.disable();
          setTimeout(() => this.router.navigate(['/login']), 2000);
        } else {
          this.apiError = res.message || 'Registration failed.';
          this.snackBar.open(this.apiError, 'Close', { duration: 4000 });
        }
      },
      error: err => {
        this.loading = false;
  this.apiError = err?.error?.message || 'Registration failed. Please try again.';
        this.snackBar.open(this.apiError ?? 'Registration failed. Please try again.', 'Close', { duration: 4000 });
      }
    });
  }
}
