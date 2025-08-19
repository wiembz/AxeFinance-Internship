import { CommonModule } from '@angular/common';
import { Component, OnInit, ViewEncapsulation } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { CreateDepartmentDto, UpdateDepartmentDto } from '../../../shared/models/department.model';
import { DepartmentService } from '../../../shared/services/department.service';

@Component({
  selector: 'app-department-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './department-form.component.html',
  styleUrl: './department-form.component.scss',
  encapsulation: ViewEncapsulation.None
})
export class DepartmentFormComponent implements OnInit {
  form: FormGroup;
  loading = false;
  saving = false;
  error = '';
  success = '';
  isEditMode = false;
  departmentId: number | null = null;

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private route: ActivatedRoute,
    private departmentService: DepartmentService
  ) {
    this.form = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
      description: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(500)]],
      departmentHeadId: [null] // Optional for now
    });
  }

  ngOnInit() {
    this.route.params.subscribe(params => {
      if (params['id']) {
        this.isEditMode = true;
        this.departmentId = +params['id'];
        this.loadDepartment();
      }
    });
  }

  loadDepartment() {
    if (!this.departmentId) return;

    this.loading = true;
    this.departmentService.getDepartment(this.departmentId).subscribe({
      next: (department) => {
        this.form.patchValue({
          name: department.name,
          description: department.description,
          departmentHeadId: department.departmentHeadId
        });
        this.loading = false;
      },
      error: (error) => {
        this.error = error.message || 'Failed to load department';
        this.loading = false;
      }
    });
  }

  onSubmit() {
    if (this.form.invalid) {
      this.markFormGroupTouched(this.form);
      return;
    }

    this.saving = true;
    this.error = '';

    const formValue = this.form.value;

    if (this.isEditMode && this.departmentId) {
      const updateDto: UpdateDepartmentDto = {
        name: formValue.name.trim(),
        description: formValue.description.trim(),
        departmentHeadId: formValue.departmentHeadId || undefined
      };

      this.departmentService.updateDepartment(this.departmentId, updateDto).subscribe({
        next: () => {
          this.router.navigate(['/admin/departments']);
        },
        error: (error) => {
          this.error = error.message || 'Failed to update department';
          this.saving = false;
        }
      });
    } else {
      const createDto: CreateDepartmentDto = {
        name: formValue.name.trim(),
        description: formValue.description.trim(),
        departmentHeadId: formValue.departmentHeadId || undefined
      };

      this.departmentService.createDepartment(createDto).subscribe({
        next: () => {
          this.router.navigate(['/admin/departments']);
        },
        error: (error) => {
          this.error = error.message || 'Failed to create department';
          this.saving = false;
        }
      });
    }
  }

  onCancel() {
    this.router.navigate(['/admin/departments']);
  }

  private markFormGroupTouched(formGroup: FormGroup) {
    Object.keys(formGroup.controls).forEach(field => {
      const control = formGroup.get(field);
      control?.markAsTouched({ onlySelf: true });
      if (control instanceof FormGroup) {
        this.markFormGroupTouched(control);
      }
    });
  }

  getFieldError(fieldName: string): string | null {
    const field = this.form.get(fieldName);
    if (field && field.invalid && field.touched) {
      if (field.errors?.['required']) {
        return `${this.getFieldLabel(fieldName)} is required`;
      }
      if (field.errors?.['minlength']) {
        return `${this.getFieldLabel(fieldName)} must be at least ${field.errors['minlength'].requiredLength} characters`;
      }
      if (field.errors?.['maxlength']) {
        return `${this.getFieldLabel(fieldName)} cannot exceed ${field.errors['maxlength'].requiredLength} characters`;
      }
    }
    return null;
  }

  private getFieldLabel(fieldName: string): string {
    const labels: Record<string, string> = {
      name: 'Department name',
      description: 'Description'
    };
    return labels[fieldName] || fieldName;
  }
}
