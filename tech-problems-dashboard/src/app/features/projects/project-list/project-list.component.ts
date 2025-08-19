import { CommonModule } from '@angular/common';
import { Component, Inject, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatChipsModule } from '@angular/material/chips';
import {
    MAT_DIALOG_DATA,
    MatDialog,
    MatDialogModule,
    MatDialogRef
} from '@angular/material/dialog';
import { MatDividerModule } from '@angular/material/divider';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { Subject, combineLatest, debounceTime, distinctUntilChanged, startWith, takeUntil } from 'rxjs';

import { AuthService } from '../../../core/auth/auth.service';
import { ProjectListItem } from '../../../shared/models/project.model';
import { DepartmentService } from '../../../shared/services/department.service';
import { ProjectListResponse, ProjectService } from '../../../shared/services/project.service';

export interface ProjectFilters {
  searchTerm: string;
  departmentId: number | null;
  isActive: boolean | null;
  sortBy: string;
  sortOrder: string;
}

@Component({
  selector: 'app-project-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ReactiveFormsModule,
    MatTableModule,
    MatPaginatorModule,
    MatSortModule,
    MatInputModule,
    MatFormFieldModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    MatChipsModule,
    MatTooltipModule,
    MatMenuModule,
    MatProgressSpinnerModule,
    MatCheckboxModule,
    MatSnackBarModule,
    MatDialogModule,
    MatDividerModule
  ],
  templateUrl: './project-list.component.html',
  styleUrls: ['./project-list.component.scss']
})
export class ProjectListComponent implements OnInit, OnDestroy {
  private projectService = inject(ProjectService);
  private departmentService = inject(DepartmentService);
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private snackBar = inject(MatSnackBar);
  private dialog = inject(MatDialog);
  private destroy$ = new Subject<void>();

  // Signals for reactive state management
  projects = signal<ProjectListItem[]>([]);
  loading = signal<boolean>(false);
  error = signal<string | null>(null);
  totalCount = signal<number>(0);
  currentPage = signal<number>(1);
  pageSize = signal<number>(12); // Increased for grid layout
  totalPages = signal<number>(0);
  hasNextPage = signal<boolean>(false);
  hasPreviousPage = signal<boolean>(false);
  selectedProjects = signal<Set<number>>(new Set());

  // View mode properties
  viewMode: 'grid' | 'list' = 'grid';
  activeCardMenu: number | null = null;
  // Lock department filter when navigated from department page
  isDepartmentLocked: boolean = false;

  // Form controls
  searchControl = new FormControl('');
  departmentControl = new FormControl<number | null>(null);
  statusControl = new FormControl<boolean | null>(null);

  // Filter state
  filters = signal<ProjectFilters>({
    searchTerm: '',
    departmentId: null,
    isActive: null,
    sortBy: 'name',
    sortOrder: 'asc'
  });

  // Departments for filter dropdown
  departments = signal<Array<{id: number, name: string}>>([]);

  // Route parameters
  departmentId: number | null = null;

  // Computed signals
  isAllSelected = computed(() => {
    const selected = this.selectedProjects();
    const currentProjects = this.projects();
    return currentProjects.length > 0 && selected.size === currentProjects.length;
  });

  isIndeterminate = computed(() => {
    const selected = this.selectedProjects();
    const currentProjects = this.projects();
    return selected.size > 0 && selected.size < currentProjects.length;
  });

  canCreateProject = computed(() => {
    const user = this.authService.currentUser;
    return user && (user.role === 'SuperAdmin' || user.role === 'Admin');
  });

  hasActiveFilters = computed(() => {
    const filters = this.filters();
    return !!(
      filters.searchTerm ||
      filters.departmentId ||
      filters.isActive !== null
    );
  });

  // Table configuration
  displayedColumns: string[] = [
    'select',
    'name',
    'description',
    'departmentName',
    'problemsCount',
    'createdBy',
    'createdDate',
    'status',
    'actions'
  ];

  // Math reference for template
  Math = Math;

  ngOnInit(): void {
    this.initializeFromRoute();
    this.setupFilterSubscriptions();
    this.loadDepartments();
    this.loadProjects();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private initializeFromRoute(): void {
    const paramDepartmentId = this.route.snapshot.params['departmentId'];
    const queryDepartmentIdRaw = this.route.snapshot.queryParamMap.get('departmentId');
    const queryDepartmentId = queryDepartmentIdRaw ? +queryDepartmentIdRaw : null;

    const resolvedDepartmentId = paramDepartmentId ? +paramDepartmentId : queryDepartmentId;

    if (resolvedDepartmentId) {
      this.departmentId = resolvedDepartmentId;
      this.isDepartmentLocked = true;
      this.departmentControl.setValue(resolvedDepartmentId);
      this.filters.update(f => ({ ...f, departmentId: resolvedDepartmentId }));
    }
  }

  private setupFilterSubscriptions(): void {
    // Search term changes
    this.searchControl.valueChanges.pipe(
      startWith(''),
      debounceTime(300),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(searchTerm => {
      this.filters.update(f => ({ ...f, searchTerm: searchTerm || '' }));
      this.resetPagination();
      this.loadProjects();
    });

    // Department filter changes
    this.departmentControl.valueChanges.pipe(
      startWith(this.departmentControl.value),
      takeUntil(this.destroy$)
    ).subscribe(departmentId => {
      this.filters.update(f => ({ ...f, departmentId }));
      this.resetPagination();
      this.loadProjects();
    });

    // Status filter changes
    this.statusControl.valueChanges.pipe(
      startWith(null),
      takeUntil(this.destroy$)
    ).subscribe(isActive => {
      this.filters.update(f => ({ ...f, isActive }));
      this.resetPagination();
      this.loadProjects();
    });
  }

  private loadDepartments(): void {
    this.departmentService.getDepartments({ pageSize: 1000 })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          const deptOptions = response.departments.map(dept => ({
            id: dept.id,
            name: dept.name
          }));
          this.departments.set(deptOptions);
        },
        error: (error) => {
          console.error('Error loading departments:', error);
        }
      });
  }

  loadProjects(): void {
    this.loading.set(true);
    this.error.set(null);
    const currentFilters = this.filters();
    const page = this.currentPage();
    const pageSize = this.pageSize();

    const params = {
      page,
      pageSize,
      searchTerm: currentFilters.searchTerm || undefined,
      departmentId: currentFilters.departmentId || undefined,
      isActive: currentFilters.isActive ?? undefined,
      sortBy: currentFilters.sortBy,
      sortOrder: currentFilters.sortOrder
    };

    this.projectService.getProjects(params)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: ProjectListResponse) => {
          this.projects.set(response.projects);
          this.totalCount.set(response.totalCount);
          this.totalPages.set(response.totalPages);
          this.hasNextPage.set(response.hasNextPage);
          this.hasPreviousPage.set(response.hasPreviousPage);
          this.loading.set(false);
        },
        error: (error) => {
          console.error('Error loading projects:', error);
          this.error.set(error?.message || 'Failed to load projects');
          this.snackBar.open('Error loading projects', 'Close', { duration: 3000 });
          this.loading.set(false);
        }
      });
  }

  onPageChange(event: PageEvent): void {
    this.currentPage.set(event.pageIndex + 1);
    this.pageSize.set(event.pageSize);
    this.loadProjects();
  }

  changePage(pageIndex: number): void {
    this.currentPage.set(pageIndex + 1);
    this.loadProjects();
  }

  onSortChange(sort: Sort): void {
    this.filters.update(f => ({
      ...f,
      sortBy: sort.active,
      sortOrder: sort.direction || 'asc'
    }));
    this.resetPagination();
    this.loadProjects();
  }

  private resetPagination(): void {
    this.currentPage.set(1);
  }

  // Selection methods
  toggleAllSelection(): void {
    const currentProjects = this.projects();
    const selected = this.selectedProjects();

    if (this.isAllSelected()) {
      this.selectedProjects.set(new Set());
    } else {
      const allIds = new Set(currentProjects.map(p => p.id));
      this.selectedProjects.set(allIds);
    }
  }

  toggleProjectSelection(projectId: number): void {
    const selected = new Set(this.selectedProjects());
    if (selected.has(projectId)) {
      selected.delete(projectId);
    } else {
      selected.add(projectId);
    }
    this.selectedProjects.set(selected);
  }

  isProjectSelected(projectId: number): boolean {
    return this.selectedProjects().has(projectId);
  }

  // Navigation methods
  navigateToCreate(): void {
    if (this.departmentId) {
      this.router.navigate(['/admin/projects/create'], {
        queryParams: { departmentId: this.departmentId }
      });
    } else {
      this.router.navigate(['/admin/projects/create']);
    }
  }

  navigateToEdit(projectId: number): void {
    this.router.navigate(['/admin/projects/edit', projectId]);
  }

  navigateToProblems(projectId: number): void {
    this.router.navigate(['/admin/projects', projectId, 'problems']);
  }

  navigateToView(projectId: number): void {
    this.router.navigate(['/projects', projectId]);
  }

  // Actions
  confirmDelete(project: ProjectListItem): void {
    if (!project.canDelete) {
      this.snackBar.open('You do not have permission to delete this project', 'Close', { duration: 3000 });
      return;
    }

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: {
        title: 'Delete Project',
        message: `Are you sure you want to delete the project "${project.name}"? This action cannot be undone.`,
        confirmText: 'Delete',
        cancelText: 'Cancel',
        confirmColor: 'warn'
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.deleteProject(project.id);
      }
    });
  }

  private deleteProject(projectId: number): void {
    this.loading.set(true);
    this.projectService.deleteProject(projectId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.snackBar.open('Project deleted successfully', 'Close', { duration: 3000 });
          this.loadProjects();
        },
        error: (error) => {
          console.error('Error deleting project:', error);
          this.snackBar.open('Error deleting project', 'Close', { duration: 3000 });
          this.loading.set(false);
        }
      });
  }

  // Bulk actions
  deleteSelectedProjects(): void {
    const selected = this.selectedProjects();
    if (selected.size === 0) return;

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: {
        title: 'Delete Projects',
        message: `Are you sure you want to delete ${selected.size} selected project(s)? This action cannot be undone.`,
        confirmText: 'Delete',
        cancelText: 'Cancel',
        confirmColor: 'warn'
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.executeBulkDelete();
      }
    });
  }

  private executeBulkDelete(): void {
    const selectedIds = Array.from(this.selectedProjects());
    const deleteRequests = selectedIds.map(id => this.projectService.deleteProject(id));

    this.loading.set(true);
    combineLatest(deleteRequests)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.snackBar.open(`${selectedIds.length} project(s) deleted successfully`, 'Close', { duration: 3000 });
          this.selectedProjects.set(new Set());
          this.loadProjects();
        },
        error: (error) => {
          console.error('Error deleting projects:', error);
          this.snackBar.open('Error deleting some projects', 'Close', { duration: 3000 });
          this.loading.set(false);
        }
      });
  }

  // Utility methods
  formatDate(dateString: string): string {
    if (!dateString) return 'N/A';

    const dateObj = new Date(dateString);
    return dateObj.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }

  getDepartmentName(departmentId: number): string {
    return this.departments().find(d => d.id === departmentId)?.name || '';
  }

  getStatusColor(isActive: boolean): string {
    return isActive ? 'primary' : 'warn';
  }

  getStatusText(isActive: boolean): string {
    return isActive ? 'Active' : 'Inactive';
  }

  clearFilters(): void {
    this.searchControl.setValue('');
    this.departmentControl.setValue(null);
    this.statusControl.setValue(null);
    this.filters.set({
      searchTerm: '',
      departmentId: null,
      isActive: null,
      sortBy: 'name',
      sortOrder: 'asc'
    });
    this.resetPagination();
    this.loadProjects();
  }

  exportSelected(): void {
    const selected = this.selectedProjects();
    if (selected.size === 0) {
      this.snackBar.open('Please select projects to export', 'Close', { duration: 3000 });
      return;
    }

    const selectedProjects = this.projects().filter(p => selected.has(p.id));
    this.exportToCSV(selectedProjects);
  }

  private exportToCSV(projects: ProjectListItem[]): void {
    const headers = ['Name', 'Description', 'Department', 'Problems Count', 'Created By', 'Created Date', 'Status'];
    const csvData = [
      headers.join(','),
      ...projects.map(p => [
        `"${p.name}"`,
        `"${p.description}"`,
        `"${p.departmentName}"`,
        p.problemsCount.toString(),
        `"${p.createdBy}"`,
        this.formatDate(p.createdDate),
        this.getStatusText(p.isActive)
      ].join(','))
    ].join('\n');

    const blob = new Blob([csvData], { type: 'text/csv' });
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = 'projects.csv';
    link.click();
    window.URL.revokeObjectURL(url);
  }

  // Template helper methods
  clearSearch(): void {
    this.searchControl.setValue('');
  }

  setViewMode(mode: 'grid' | 'list'): void {
    this.viewMode = mode;
  }

  trackByProject(index: number, project: ProjectListItem): number {
    return project.id;
  }

  getProjectIconClass(project: ProjectListItem): string {
    if (!project.isActive) return 'project-inactive';
    if (project.problemsCount > 0) return 'project-has-problems';
    return 'project-default';
  }

  toggleCardMenu(projectId: number): void {
    this.activeCardMenu = this.activeCardMenu === projectId ? null : projectId;
  }

  toggleSort(field: string): void {
    const currentFilters = this.filters();
    if (currentFilters.sortBy === field) {
      this.filters.update(f => ({
        ...f,
        sortOrder: f.sortOrder === 'asc' ? 'desc' : 'asc'
      }));
    } else {
      this.filters.update(f => ({
        ...f,
        sortBy: field,
        sortOrder: 'asc'
      }));
    }
    this.resetPagination();
    this.loadProjects();
  }

  getResultStart(): number {
    return (this.currentPage() - 1) * this.pageSize() + 1;
  }

  getResultEnd(): number {
    return Math.min(this.currentPage() * this.pageSize(), this.totalCount());
  }

  getPaginationArray(): (number | string)[] {
    const currentPage = this.currentPage();
    const totalPages = this.totalPages();
    const maxVisiblePages = 5;
    const pages: (number | string)[] = [];

    if (totalPages <= maxVisiblePages) {
      for (let i = 1; i <= totalPages; i++) {
        pages.push(i);
      }
    } else {
      if (currentPage <= 3) {
        for (let i = 1; i <= 3; i++) pages.push(i);
        if (totalPages > 4) pages.push('...');
        pages.push(totalPages);
      } else if (currentPage >= totalPages - 2) {
        pages.push(1);
        if (totalPages > 4) pages.push('...');
        for (let i = totalPages - 2; i <= totalPages; i++) pages.push(i);
      } else {
        pages.push(1);
        pages.push('...');
        for (let i = currentPage - 1; i <= currentPage + 1; i++) pages.push(i);
        pages.push('...');
        pages.push(totalPages);
      }
    }

    return pages;
  }
}

// Confirm Dialog Component
export interface ConfirmDialogData {
  title: string;
  message: string;
  confirmText: string;
  cancelText: string;
  confirmColor?: string;
}

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>{{ data.title }}</h2>
    <mat-dialog-content>
      <p>{{ data.message }}</p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="onCancel()">{{ data.cancelText }}</button>
      <button mat-raised-button [color]="data.confirmColor || 'primary'" (click)="onConfirm()">
        {{ data.confirmText }}
      </button>
    </mat-dialog-actions>
  `
})
export class ConfirmDialogComponent {
  constructor(
    public dialogRef: MatDialogRef<ConfirmDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: ConfirmDialogData
  ) {}

  onConfirm(): void {
    this.dialogRef.close(true);
  }

  onCancel(): void {
    this.dialogRef.close(false);
  }
}
