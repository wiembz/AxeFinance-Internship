import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { PaginatedProblemsResponse, ProblemListItem } from '../../../shared/models/problem.model';
import { Project } from '../../../shared/models/project.model';
import { ProblemService } from '../../../shared/services/problem.service';
import { ProjectService } from '../../../shared/services/project.service';

type ExtractedProblemsData = {
  problems: ProblemListItem[];
  totalCount: number;
  totalPages: number;
  currentPage: number;
};

@Component({
  selector: 'app-problem-list',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './problem-list.component.html',
  styleUrls: ['./problem-list.component.scss']
})
export class ProblemListComponent implements OnInit {
  loading = false;
  error = '';
  problems: ProblemListItem[] = [];
  project: Project | null = null;
  projectId: number | null = null;

  // UI state
  activeCardMenu: number | null = null;

  // Pagination
  currentPage = 1;
  pageSize = 12;
  totalCount = 0;
  totalPages = 0;

  // Filters
  searchTerm = '';
  selectedStatus: string = '';
  selectedAssignee: string = '';

  // Make Math available in template
  Math = Math;

  // Utility to extract problems and pagination from backend response
  private extractProblemsData(data: PaginatedProblemsResponse): ExtractedProblemsData {
    let problems: ProblemListItem[] = [];
    if (Array.isArray((data as any).Problems)) {
      problems = (data as any).Problems;
    } else if (Array.isArray((data as any).problems)) {
      problems = (data as any).problems;
    } else if (Array.isArray((data as any).items)) {
      problems = (data as any).items;
    } else if (Array.isArray((data as any).Page)) {
      problems = (data as any).Page;
    }
    const totalCount = data.TotalCount ?? data.totalCount ?? data.totalCount ?? 0;
    const totalPages = data.TotalPages ?? data.totalPages ?? 0;
    const currentPage = data.Page ?? data.page ?? data.currentPage ?? 1;
    return { problems, totalCount, totalPages, currentPage };
  }

  constructor(
    private problemService: ProblemService,
    private projectService: ProjectService,
    private route: ActivatedRoute,
    private router: Router
  ) {}
  ngOnInit(): void {
    // Check if this is a project-specific problems page
    this.route.params.subscribe(params => {
      if (params['projectId']) {
        this.projectId = +params['projectId'];
        console.log('ngOnInit: projectId detected', this.projectId);
        this.loadProject();
      } else {
        console.log('ngOnInit: no projectId, loading all problems');
        this.loadProblems();
      }
    });
  }

  private loadProject(): void {
    if (!this.projectId) return;

    this.loading = true;
    this.projectService.getProject(this.projectId).subscribe({
      next: (project) => {
        this.project = project;
        this.loadProblems();
      },
      error: (error) => {
        console.error('Error loading project:', error);
        this.error = 'Failed to load project details';
        this.loading = false;
      }
    });
  }

  private loadProblems(): void {
    this.loading = true;
    this.error = '';

    const filters: any = {
      searchTerm: this.searchTerm || undefined,
      isActive: true
    };

    if (this.projectId) {
      filters.projectId = this.projectId;
    }

    if (this.selectedStatus) {
      filters.status = this.selectedStatus;
    }

    if (this.selectedAssignee) {
      filters.assignedTo = +this.selectedAssignee;
    }

    console.log('loadProblems: filters', filters);
    this.problemService.getProblems(this.currentPage, this.pageSize, filters).subscribe({
      next: (response) => {
        console.log('loadProblems: API response', response);
        this.loading = false;
        if (response.success && response.data) {
          const { problems, totalCount, totalPages, currentPage } = this.extractProblemsData(response.data);
          this.problems = problems;
          this.totalCount = totalCount;
          this.totalPages = totalPages;
          this.currentPage = currentPage;
        } else {
          this.error = response.message || 'Failed to load problems';
          this.problems = [];
        }
      },
      error: (error) => {
        console.error('Error loading problems:', error);
        this.loading = false;
        this.error = 'Failed to load problems. Please check your connection and try again.';
        this.problems = [];
      }
    });
  }

  onSearch(): void {
    this.currentPage = 1;
    this.loadProblems();
  }

  onStatusFilter(status: string): void {
    this.selectedStatus = status;
    this.currentPage = 1;
    this.loadProblems();
  }

  onAssigneeFilter(assignee: string): void {
    this.selectedAssignee = assignee;
    this.currentPage = 1;
    this.loadProblems();
  }

  onPageChange(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
      this.loadProblems();
      // Scroll to top of the page
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  }

  onRefresh(): void {
    this.loadProblems();
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.selectedStatus = '';
    this.selectedAssignee = '';
    this.currentPage = 1;
    this.loadProblems();
  }

  viewProblem(problemId: number): void {
    // Navigate to problem details page
    this.router.navigate(['/problems', problemId]);
  }

  editProblem(problemId: number): void {
    // Navigate to problem edit page
    this.router.navigate(['/problems', problemId, 'edit']);
  }

  getStatusClass(status: string): string {
    if (!status || typeof status !== 'string') {
      return 'status-open';
    }
    switch (status.toLowerCase()) {
      case 'open': return 'status-open';
      case 'inprogress': return 'status-progress';
      case 'underreview': return 'status-review';
      case 'resolved': return 'status-resolved';
      case 'closed': return 'status-closed';
      case 'rejected': return 'status-rejected';
      default: return 'status-open';
    }
  }

  getPaginationArray(): number[] {
    const pages: number[] = [];
    const maxPagesToShow = 5;
    const halfRange = Math.floor(maxPagesToShow / 2);

    let startPage = Math.max(1, this.currentPage - halfRange);
    let endPage = Math.min(this.totalPages, startPage + maxPagesToShow - 1);

    // Adjust start page if we're near the end
    if (endPage - startPage < maxPagesToShow - 1) {
      startPage = Math.max(1, endPage - maxPagesToShow + 1);
    }

    for (let i = startPage; i <= endPage; i++) {
      pages.push(i);
    }

    return pages;
  }

  getStartRecord(): number {
    return Math.max(1, (this.currentPage - 1) * this.pageSize + 1);
  }

  getEndRecord(): number {
    return Math.min(this.currentPage * this.pageSize, this.totalCount);
  }

  // Helper method for tracking items in ngFor
  trackByProblemId(index: number, problem: ProblemListItem): number {
    return problem.id;
  }

  // Card menu management
  toggleCardMenu(problemId: number): void {
    this.activeCardMenu = this.activeCardMenu === problemId ? null : problemId;
  }

  // Method to format status for display
  formatStatus(status: string): string {
    switch (status.toLowerCase()) {
      case 'inprogress': return 'In Progress';
      case 'underreview': return 'Under Review';
      default: return status;
    }
  }

  // Method to get problem priority color (since all are critical now)
  getPriorityColor(): string {
    return '#dc2626'; // Red color for critical
  }

  // Method to check if user can edit problem (placeholder for future implementation)
  canEditProblem(problem: ProblemListItem): boolean {
    // This would typically check user permissions
    // For now, return true for all problems
    return true;
  }

  // Method to get relative time (could be implemented with a pipe later)
  getRelativeTime(date: Date): string {
    const now = new Date();
    const problemDate = new Date(date);
    const diffInHours = Math.floor((now.getTime() - problemDate.getTime()) / (1000 * 60 * 60));

    if (diffInHours < 1) {
      return 'Just now';
    } else if (diffInHours < 24) {
      return `${diffInHours} hour${diffInHours > 1 ? 's' : ''} ago`;
    } else {
      const diffInDays = Math.floor(diffInHours / 24);
      return `${diffInDays} day${diffInDays > 1 ? 's' : ''} ago`;
    }
  }
}
