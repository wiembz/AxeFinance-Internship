import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { DepartmentListItem } from '../../../shared/models/department.model';
import { DepartmentService } from '../../../shared/services/department.service';

@Component({
  selector: 'app-department-list',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './department-list.component.html',
  styleUrls: ['./department-list.component.scss']
})
export class DepartmentListComponent implements OnInit {
  // Make Math available in template
  Math = Math;

  departments: DepartmentListItem[] = [];
  loading = false;
  error = '';

  // View mode and UI state
  viewMode: 'grid' | 'list' = 'grid';
  activeCardMenu: number | null = null;

  // Pagination
  currentPage = 1;
  pageSize = 12; // Increased for grid layout
  totalCount = 0;
  totalPages = 0;

  // Filters
  searchTerm = '';

  // Permissions
  canManageDepartments = true; // This should come from auth service/permissions

  constructor(
    private departmentService: DepartmentService,
    private router: Router
  ) {}

  ngOnInit() {
    this.loadDepartments();
  }

  loadDepartments() {
    this.loading = true;
    this.error = '';

    this.departmentService.getDepartments({
      page: this.currentPage,
      pageSize: this.pageSize,
      search: this.searchTerm || undefined
    }).subscribe({
      next: (response) => {
        this.departments = response.departments;
        this.totalCount = response.totalCount;
        this.totalPages = response.totalPages;
        this.loading = false;
      },
      error: (error) => {
        this.error = error.message || 'Failed to load departments';
        this.loading = false;
      }
    });
  }

  // View mode management
  setViewMode(mode: 'grid' | 'list'): void {
    this.viewMode = mode;
  }

  // Search functionality
  onSearch() {
    this.currentPage = 1;
    this.loadDepartments();
  }

  clearSearch(): void {
    this.searchTerm = '';
    this.onSearch();
  }

  onFilterChange() {
    this.currentPage = 1;
    this.loadDepartments();
  }

  // Pagination helpers
  onPageChange(page: number) {
    this.currentPage = page;
    this.loadDepartments();
  }

  getResultStart(): number {
    return (this.currentPage - 1) * this.pageSize + 1;
  }

  getResultEnd(): number {
    return Math.min(this.currentPage * this.pageSize, this.totalCount);
  }

  getPaginationArray(): number[] {
    const maxPages = 5;
    const half = Math.floor(maxPages / 2);
    let start = Math.max(1, this.currentPage - half);
    let end = Math.min(this.totalPages, start + maxPages - 1);

    if (end - start + 1 < maxPages) {
      start = Math.max(1, end - maxPages + 1);
    }

    const pages = [];
    for (let i = start; i <= end; i++) {
      pages.push(i);
    }
    return pages;
  }

  // Card management
  toggleCardMenu(departmentId: number): void {
    this.activeCardMenu = this.activeCardMenu === departmentId ? null : departmentId;
  }

  // Department navigation - View projects for specific department
  viewDepartmentProjects(department: DepartmentListItem): void {
    // Navigate to projects list filtered by this department
    this.router.navigate(['/admin/projects'], {
      queryParams: {
        departmentId: department.id,
        departmentName: department.name
      }
    });
  }

  // Department helpers
  getAvatarText(name: string): string {
    if (!name) return 'DE';
    const words = name.trim().split(' ').filter(word => word.length > 0);
    if (words.length >= 2) {
      return (words[0][0] + words[1][0]).toUpperCase();
    }
    return name.substring(0, 2).toUpperCase();
  }

  trackByDepartment(index: number, department: DepartmentListItem): string | number {
    return department.id;
  }

  // Department actions
  createDepartment() {
    // Navigate to create department - handled by routerLink in template
  }

  editDepartment(id: string) {
    // Navigate to edit department - handled by routerLink in template
  }

  confirmDelete(department: DepartmentListItem): void {
    this.activeCardMenu = null; // Close menu
    this.deleteDepartment(department);
  }

  deleteDepartment(department: DepartmentListItem) {
    if (!confirm(`Are you sure you want to delete the department "${department.name}"? This action cannot be undone.`)) {
      return;
    }

    this.departmentService.deleteDepartment(department.id).subscribe({
      next: () => {
        this.loadDepartments(); // Reload the list
      },
      error: (error) => {
        alert(`Failed to delete department: ${error.message}`);
      }
    });
  }

  formatDate(date: string | Date): string {
    if (!date) return 'N/A';

    const dateObj = typeof date === 'string' ? new Date(date) : date;
    return dateObj.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }
}
