import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DepartmentDropdownItem } from '../../../shared/models/department.model';
import { CreateProjectDto, UpdateProjectDto } from '../../../shared/models/project.model';
import { DepartmentService } from '../../../shared/services/department.service';
import { ProjectService } from '../../../shared/services/project.service';

@Component({
  selector: 'app-project-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './project-form.component.html',
  styleUrls: ['./project-form.component.scss']
})
export class ProjectFormComponent implements OnInit {
  form: FormGroup;
  loading = false;
  saving = false;
  error = '';
  success = '';
  isEditMode = false;
  projectId: number | null = null;
  departments: DepartmentDropdownItem[] = [];
  preselectedDepartmentId: number | null = null;

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private route: ActivatedRoute,
    private projectService: ProjectService,
    private departmentService: DepartmentService
  ) {
    this.form = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(200)]],
      description: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(2000)]],
      departmentId: ['', [Validators.required]]
    });
  }

  ngOnInit() {
    // Check for preselected department from query params
    this.route.queryParams.subscribe(params => {
      if (params['departmentId']) {
        this.preselectedDepartmentId = +params['departmentId'];
      }
    });

    this.route.params.subscribe(params => {
      if (params['id']) {
        this.isEditMode = true;
        this.projectId = +params['id'];
      }
    });

    this.loadDepartments();
  }

  loadDepartments() {
    this.departmentService.getDepartmentsForDropdown().subscribe({
      next: (departments) => {
        this.departments = departments;

        // If we have a preselected department, set it in the form
        if (this.preselectedDepartmentId) {
          this.form.patchValue({ departmentId: this.preselectedDepartmentId });
        }

        // If in edit mode, load the project data
        if (this.isEditMode && this.projectId) {
          this.loadProject();
        }
      },
      error: (error) => {
        console.error('Error loading departments:', error);
        this.error = 'Failed to load departments. Please refresh the page and try again.';
      }
    });
  }

  loadProject() {
    if (!this.projectId) return;

    this.loading = true;
    this.projectService.getProject(this.projectId).subscribe({
      next: (project) => {
        this.form.patchValue({
          name: project.name,
          description: project.description,
          departmentId: project.departmentId
        });
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading project:', error);
        this.error = 'Failed to load project data. Please refresh the page and try again.';
        this.loading = false;
      }
    });
  }

  onSubmit() {
    if (this.form.valid && !this.saving) {
      this.saving = true;
      this.error = '';

      const formValue = this.form.value;

      if (this.isEditMode && this.projectId) {
        // Update existing project
        const updateDto: UpdateProjectDto = {
          name: formValue.name.trim(),
          description: formValue.description.trim()
        };

        this.projectService.updateProject(this.projectId, updateDto).subscribe({
          next: () => {
            this.navigateBack();
          },
          error: (error) => {
            console.error('Error updating project:', error);
            this.error = error?.error?.message || 'Failed to update project. Please try again.';
            this.saving = false;
          }
        });
      } else {
        // Create new project
        const createDto: CreateProjectDto = {
          name: formValue.name.trim(),
          description: formValue.description.trim(),
          departmentId: +formValue.departmentId
        };

        this.projectService.createProject(createDto).subscribe({
          next: () => {
            this.navigateBack();
          },
          error: (error) => {
            console.error('Error creating project:', error);
            this.error = error?.error?.message || 'Failed to create project. Please try again.';
            this.saving = false;
          }
        });
      }
    } else {
      // Mark all fields as touched to show validation errors
      Object.keys(this.form.controls).forEach(key => {
        this.form.get(key)?.markAsTouched();
      });
    }
  }

  onCancel() {
    this.navigateBack();
  }

  private navigateBack() {
    if (this.preselectedDepartmentId) {
      this.router.navigate(['/admin/projects/department', this.preselectedDepartmentId]);
    } else {
      this.router.navigate(['/admin/projects']);
    }
  }

  getFieldError(fieldName: string): string | null {
    const field = this.form.get(fieldName);
    if (field && field.invalid && field.touched) {
      if (field.errors?.['required']) {
        return `${this.getFieldDisplayName(fieldName)} is required`;
      }
      if (field.errors?.['minlength']) {
        const minLength = field.errors['minlength'].requiredLength;
        return `${this.getFieldDisplayName(fieldName)} must be at least ${minLength} characters`;
      }
      if (field.errors?.['maxlength']) {
        const maxLength = field.errors['maxlength'].requiredLength;
        return `${this.getFieldDisplayName(fieldName)} must not exceed ${maxLength} characters`;
      }
    }
    return null;
  }

  private getFieldDisplayName(fieldName: string): string {
    const fieldNames: { [key: string]: string } = {
      name: 'Project name',
      description: 'Description',
      departmentId: 'Department'
    };
    return fieldNames[fieldName] || fieldName;
  }

  getDepartmentName(): string {
    if (this.preselectedDepartmentId && this.departments.length > 0) {
      const dept = this.departments.find(d => d.id === this.preselectedDepartmentId);
      return dept?.name || '';
    }
    return '';
  }
}
