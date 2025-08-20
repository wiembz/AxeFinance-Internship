import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { Problem } from '../../../shared/models/problem.model';
import { ProblemService } from '../../../shared/services/problem.service';

@Component({
  selector: 'app-problem-details',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './problem-details.component.html',
  styleUrls: ['./problem-details.component.scss']
})
export class ProblemDetailsComponent implements OnInit {
  problem: Problem | null = null;
  loading = false;
  error = '';

  constructor(
    private route: ActivatedRoute,
    public router: Router,
    private problemService: ProblemService
  ) {}

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
    if (id) {
      this.loading = true;
      this.problemService.getProblemById(+id).subscribe({
        next: (response) => {
          this.problem = response.data || null;
          this.loading = false;
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
    this.router.navigate(['/problems']);
  }
}
