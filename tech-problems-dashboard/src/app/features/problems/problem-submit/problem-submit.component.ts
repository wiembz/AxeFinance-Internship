// ...existing imports...



  // ...existing code...
import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { DepartmentListItem } from '../../../shared/models/department.model';
import { ProblemSubmission, ProblemTag } from '../../../shared/models/problem.model';
import { ProjectListItem } from '../../../shared/models/project.model';
import { DepartmentListResponse, DepartmentService } from '../../../shared/services/department.service';
import { ProblemService } from '../../../shared/services/problem.service';
import { ProjectListResponse, ProjectService } from '../../../shared/services/project.service';

@Component({
  selector: 'app-problem-submit',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './problem-submit.component.html',
  styleUrls: ['./problem-submit.component.scss']
})
export class ProblemSubmitComponent implements OnInit {
  problemForm!: FormGroup;
  departments: DepartmentListItem[] = [];
  allProjects: ProjectListItem[] = [];
  projects: ProjectListItem[] = [];
  availableTags: { id: string; name: string }[] = [];
  selectedFile: File | null = null;
  filePreviewUrl: string | null = null;
  submitting = false;
  loading = false;
  success = '';
  error = '';

  isEditMode = false;
  problemId: number | null = null;

  constructor(
    private fb: FormBuilder,
    private problemService: ProblemService,
    private departmentService: DepartmentService,
    private projectService: ProjectService,
    private router: Router,
    private route: ActivatedRoute,
    private cdr: ChangeDetectorRef
  ) {}

  isTagSelected(tagName: string): boolean {
    const tagsValue = this.problemForm.get('tags')?.value || '';
    return tagsValue.split(',').map((t: string) => t.trim().toLowerCase()).includes(tagName.toLowerCase());
  }

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      if (params['id']) {
        this.isEditMode = true;
        this.problemId = +params['id'];
        this.initializeForm();
        this.loadDepartments();
        this.loadProjects();
        this.loadPopularTags();
        this.loadProblem();
      } else {
        this.isEditMode = false;
        this.problemId = null;
        this.initializeForm();
        this.loadDepartments();
        this.loadProjects();
        this.loadPopularTags();
      }
    });
  }

  private loadProblem(): void {
    if (!this.problemId) return;
    this.loading = true;
    this.problemService.getProblemById(this.problemId).subscribe({
      next: (response) => {
        const problem = response.data;
        if (problem) {
          this.problemForm.patchValue({
            title: problem.title,
            description: problem.description,
            departmentId: problem.departmentId,
            projectId: problem.projectId,
            tags: Array.isArray(problem.tags) ? problem.tags.join(', ') : (problem.tags || ''),
            azureLink: problem.azureLink || '',
            assignedToUserId: problem.assignedToUserId ? problem.assignedToUserId.toString() : ''
          });
          // If there are tags, update the tagsArray
          // File attachments: for edit, do not pre-load file, just show download link if needed (not implemented here)
        }
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        this.error = error.message || 'Failed to load problem';
        this.loading = false;
      }
    });
  }

  private initializeForm(): void {
    this.problemForm = this.fb.group({
      title: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(200)]],
      description: ['', [Validators.required, Validators.minLength(20), Validators.maxLength(2000)]],
      departmentId: ['', Validators.required],
      projectId: [{value: '', disabled: true}, Validators.required],
      tags: [''],
      customTag: [''],
      azureLink: [''],
      assignedToUserId: ['']
    });

    // Subscribe to department changes to enable/disable and filter project field
    this.problemForm.get('departmentId')?.valueChanges.subscribe(departmentId => {
      const projectControl = this.problemForm.get('projectId');
      if (departmentId) {
        this.projects = this.allProjects.filter(p => p.departmentId === parseInt(departmentId));
        projectControl?.enable();
      } else {
        this.projects = [];
        projectControl?.disable();
        projectControl?.setValue('');
      }
    });
  }

  private loadDepartments(): void {
    this.departmentService.getDepartments().subscribe({
      next: (response: DepartmentListResponse) => {
        this.departments = response.departments;
      },
      error: (error: any) => {
        console.error('Error loading departments:', error);
      }
    });
  }

  private loadProjects(): void {
    this.projectService.getProjects().subscribe({
      next: (response: ProjectListResponse) => {
        this.allProjects = response.projects;
        this.projects = [];
      },
      error: (error: any) => {
        console.error('Error loading projects:', error);
      }
    });
  }

  private loadPopularTags(): void {
    this.problemService.getPopularTags().subscribe({
      next: (response) => {
        this.availableTags = response.data.map((tag: ProblemTag) => ({
          id: tag.id.toString(),
          name: tag.name
        }));
      },
      error: (error: any) => {
        console.error('Error loading tags:', error);
        // Fallback tags
        this.availableTags = [
          { id: '1', name: 'bug' },
          { id: '2', name: 'feature' },
          { id: '3', name: 'ui' },
          { id: '4', name: 'performance' }
        ];
      }
    });
  }

  onFileSelected(event: Event): void {
    const target = event.target as HTMLInputElement;
    if (this.filePreviewUrl) {
      URL.revokeObjectURL(this.filePreviewUrl);
      this.filePreviewUrl = null;
    }
    if (target.files && target.files.length > 0) {
      this.selectedFile = target.files[0];
      if (this.selectedFile.type.startsWith('image/')) {
        this.filePreviewUrl = URL.createObjectURL(this.selectedFile);
      }
    } else {
      this.selectedFile = null;
    }
    setTimeout(() => this.cdr.detectChanges());
  }

  removeFile(): void {
    if (this.filePreviewUrl) {
      URL.revokeObjectURL(this.filePreviewUrl);
      this.filePreviewUrl = null;
    }
    this.selectedFile = null;
  }

  async onSubmit(): Promise<void> {
    if (this.problemForm.valid) {
      this.submitting = true;
      this.error = '';
      // Debug: log AzureLink value before submit
      console.log('AzureLink value:', this.problemForm.get('azureLink')?.value);
      this.success = '';
      try {
        const formValue = this.problemForm.getRawValue();
        const selectedProject = this.allProjects.find(p => p.id === parseInt(formValue.projectId));
        if (!selectedProject) {
          this.error = 'Selected project is invalid.';
          this.submitting = false;
          return;
        }
        // Always use the departmentId from the selected project for referential integrity
        const departmentId = selectedProject.departmentId;
        const tags = formValue.tags ? formValue.tags.split(',').map((tag: string) => tag.trim()).filter((t: string) => t) : [];
        const problemSubmission: ProblemSubmission = {
          title: formValue.title,
          description: formValue.description,
          departmentId: departmentId,
          projectId: selectedProject.id,
          tags,
          azureLink: formValue.azureLink || undefined,
          assignedToUserId: formValue.assignedToUserId ? parseInt(formValue.assignedToUserId) : undefined,
          attachments: this.selectedFile ? [this.selectedFile] : []
        };
        if (this.isEditMode && this.problemId) {
          // Update problem (implement updateProblem in ProblemService)
          const response = await this.problemService.updateProblem(this.problemId, problemSubmission);
          if (response.success) {
            this.success = 'Problem updated successfully!';
            this.submitting = false;
            setTimeout(() => {
              this.router.navigate(['/problems', this.problemId]);
            }, 1200);
          } else {
            this.error = response.message || 'Failed to update problem. Please try again.';
            this.submitting = false;
          }
        } else {
          // Create new problem
          const response = await this.problemService.createProblem(problemSubmission);
          if (response.success && response.data && response.data.id) {
            this.success = 'Problem submitted successfully!';
            this.submitting = false;
            setTimeout(() => {
              this.router.navigate(['/problems', response.data.id]);
            }, 1200);
          } else {
            this.error = response.message || 'Failed to submit problem. Please try again.';
            this.submitting = false;
          }
        }
      } catch (error: any) {
        this.error = error.message || 'Failed to submit problem. Please try again.';
        this.submitting = false;
      }
    } else {
      this.markFormGroupTouched(this.problemForm);
    }
  }

  private markFormGroupTouched(formGroup: FormGroup): void {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      control?.markAsTouched();

      if (control instanceof FormGroup) {
        this.markFormGroupTouched(control);
      }
    });
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.problemForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  getFieldError(fieldName: string): string {
    const field = this.problemForm.get(fieldName);
    if (field && field.errors && (field.dirty || field.touched)) {
      if (field.errors['required']) {
        return `${fieldName} is required`;
      }
      if (field.errors['minlength']) {
        return `${fieldName} must be at least ${field.errors['minlength'].requiredLength} characters`;
      }
      if (field.errors['maxlength']) {
        return `${fieldName} must not exceed ${field.errors['maxlength'].requiredLength} characters`;
      }
    }
    return '';
  }

  clearForm(): void {
    this.problemForm.reset();
    this.removeFile();
    this.error = '';
    this.success = '';
    this.initializeForm();
  }

  // Additional methods for template functionality
  onDepartmentChange(): void {
    // Reset project selection when department changes
    this.problemForm.get('projectId')?.setValue('');
  }

  onTagChange(tag: { id: string; name: string }, event: Event): void {
    const checkbox = event.target as HTMLInputElement;
    const tagsControl = this.problemForm.get('tags');
    let currentTags = tagsControl?.value ? tagsControl.value.split(',').map((t: string) => t.trim()).filter((t: string) => t) : [];

    if (checkbox.checked) {
      if (!currentTags.includes(tag.name)) {
        currentTags.push(tag.name);
      }
    } else {
      currentTags = currentTags.filter((t: string) => t !== tag.name);
    }

    tagsControl?.setValue(currentTags.join(', '));
  }

  addCustomTag(): void {
    const customTagControl = this.problemForm.get('customTag');
    const tagsControl = this.problemForm.get('tags');
    const customTagValue = customTagControl?.value?.trim();

    if (!customTagValue) {
      return;
    }

    // Get current tags
    const currentTags = tagsControl?.value ?
      tagsControl.value.split(',').map((t: string) => t.trim()).filter((t: string) => t) : [];

    // Check if tag already exists (case-insensitive)
    const tagExists = currentTags.some((tag: string) =>
      tag.toLowerCase() === customTagValue.toLowerCase()
    );

    if (!tagExists) {
      // Add the new tag
      currentTags.push(customTagValue);
      tagsControl?.setValue(currentTags.join(', '));
    }

    // Clear the custom tag input
    customTagControl?.setValue('');
  }

  get tagsArray() {
    // Return a mock FormArray-like structure for the template
    const tagsValue = this.problemForm.get('tags')?.value || '';
    const tags = tagsValue.split(',').map((t: string) => t.trim()).filter((t: string) => t);
    return {
      length: tags.length,
      controls: tags.map((tag: string) => ({ value: tag }))
    };
  }

  removeTag(index: number): void {
    const tagsControl = this.problemForm.get('tags');
    const currentTags = tagsControl?.value ? tagsControl.value.split(',').map((t: string) => t.trim()).filter((t: string) => t) : [];
    currentTags.splice(index, 1);
    tagsControl?.setValue(currentTags.join(', '));
  }

  onFileSelect(event: Event): void {
    this.onFileSelected(event);
  }

  getFileIcon(file: File): string {
    const extension = file.name.split('.').pop()?.toLowerCase();
    switch (extension) {
      case 'pdf': return '📄';
      case 'doc':
      case 'docx': return '📝';
      case 'xls':
      case 'xlsx': return '📊';
      case 'jpg':
      case 'jpeg':
      case 'png':
      case 'gif': return '🖼️';
      case 'zip':
      case 'rar': return '📦';
      default: return '📎';
    }
  }

  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }

  getFilePreview(): string | null {
    return this.filePreviewUrl;
  }
}
