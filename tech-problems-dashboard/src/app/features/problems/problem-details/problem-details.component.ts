import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { Problem } from '../../../shared/models/problem.model';
import { ProblemService } from '../../../shared/services/problem.service';
import { Solution, SolutionService } from '../../../shared/services/solution.service';
import { FileTypePipe } from './file-type.pipe';

@Component({
  selector: 'app-problem-details',
  standalone: true,
  imports: [CommonModule, RouterModule, FileTypePipe],
  templateUrl: './problem-details.component.html',
  styleUrls: ['./problem-details.component.scss']
})
export class ProblemDetailsComponent implements OnInit {
  problem: Problem | null = null;
  loading = false;
  error = '';
  solutions: Solution[] = [];
  solutionsLoading = false;

  constructor(
    private route: ActivatedRoute,
    public router: Router,
  private problemService: ProblemService,
  private solutionService: SolutionService,
    private sanitizer: DomSanitizer
  ) {}

  /**
   * Returns a trusted resource URL for use in iframes (prevents Angular security errors)
   */
  getSafeUrl(url: string): SafeResourceUrl {
    return this.sanitizer.bypassSecurityTrustResourceUrl(url);
  }

  /**
   * Returns a CSS class for the problem status badge.
   */
  getStatusClass(status: string): string {
    if (!status) return 'unknown';
    return status.toLowerCase().replace(/\s+/g, '-');
  }

  /**
   * Formats the status string for display (capitalize first letter).
   */
  formatStatus(status: string): string {
    if (!status) return '';
    return status.charAt(0).toUpperCase() + status.slice(1).toLowerCase();
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    const backendBaseUrl = 'http://localhost:5108'; // Change if your backend runs elsewhere
    if (id) {
      this.loading = true;
      this.problemService.getProblemById(+id).subscribe({
        next: (response) => {
          this.problem = response.data || null;
          // Map Status (capital S) to status (lowercase s) if needed
          if (this.problem && (this.problem as any).Status && !(this.problem as any).status) {
            (this.problem as any).status = (this.problem as any).Status;
          }
          // Normalize tags to always be an array
          if (this.problem) {
            if (typeof this.problem.tags === 'string') {
              const tagStr = this.problem.tags as string;
              this.problem.tags = tagStr.split(',').map(t => t.trim()).filter(t => t.length > 0);
            } else if (!Array.isArray(this.problem.tags)) {
              this.problem.tags = [];
            }
            // Ensure attachmentPath is a full backend URL
            if (this.problem.attachmentPath) {
              let path = this.problem.attachmentPath.replace(/^\\+|^\/+/, '');
              if (path.startsWith('uploads/')) {
                path = '/' + path;
              } else if (!path.startsWith('/uploads/')) {
                path = '/uploads/' + path;
              } else {
                // already starts with /uploads/
              }
              // Prepend backend URL if not already absolute
              if (!path.startsWith('http')) {
                this.problem.attachmentPath = backendBaseUrl + path;
              } else {
                this.problem.attachmentPath = path;
              }
            }
            // For multiple attachments (if present)
            if (this.problem.attachments && Array.isArray(this.problem.attachments)) {
              this.problem.attachments = this.problem.attachments.map(att => {
                let fileUrl = att.fileUrl || '';
                if (fileUrl) {
                  let attPath = fileUrl.replace(/^\\+|^\/+/, '');
                  if (attPath.startsWith('uploads/')) {
                    attPath = '/' + attPath;
                  } else if (!attPath.startsWith('/uploads/')) {
                    attPath = '/uploads/' + attPath;
                  }
                  if (!attPath.startsWith('http')) {
                    fileUrl = backendBaseUrl + attPath;
                  } else {
                    fileUrl = attPath;
                  }
                }
                return { ...att, fileUrl };
              });
            }
          }
          this.loading = false;
          // Fetch solutions for this problem
          if (this.problem && this.problem.id) {
            this.solutionsLoading = true;
            this.solutionService.getSolutionsByProblem(this.problem.id).subscribe({
              next: (res) => {
                this.solutions = res.data || [];
                this.solutionsLoading = false;
              },
              error: () => {
                this.solutions = [];
                this.solutionsLoading = false;
              }
            });
          }
        },
        error: (err) => {
          this.error = 'Failed to load problem details.';
          this.loading = false;
        }
      });
    } else {
      this.error = 'No problem ID provided.';
    }
  }

  goBack() {
    // If problem has projectId, go to project problem list, else fallback
    if (this.problem && this.problem.projectId) {
      this.router.navigate([`/admin/projects/${this.problem.projectId}/problems`]);
    } else {
      this.router.navigate(['/problems']);
    }
  }

  editProblem() {
    if (this.problem && this.problem.id) {
      // Use relative navigation if inside a child route, or absolute as fallback
      this.router.navigate([`/problems/${this.problem.id}/edit`]);
    }
  }

  addSolution() {
    if (this.problem && this.problem.id) {
      this.router.navigate(['/solutions/attribute', this.problem.id]);
    }
  }
}
