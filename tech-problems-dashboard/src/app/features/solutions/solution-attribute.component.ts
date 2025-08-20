import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, Input, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { environment } from '../../environments/environment';

@Component({
  selector: 'app-solution-attribute',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './solution-attribute.component.html',
  styleUrls: ['./solution-attribute.component.scss']
})
export class SolutionAttributeComponent implements OnInit {
  @Input() problemId?: number;
  solutionForm!: FormGroup;
  loading = false;
  error = '';
  success = false;

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    public router: Router,
    private http: HttpClient
  ) {}

  ngOnInit(): void {
    // Get problemId from route if not provided as input
    if (!this.problemId) {
      this.problemId = Number(this.route.snapshot.paramMap.get('id'));
    }
    this.solutionForm = this.fb.group({
      title: ['', [Validators.required, Validators.maxLength(100)]],
      description: ['', [Validators.required, Validators.maxLength(2000)]],
      attachment: [null]
    });
  }

  onFileChange(event: any) {
    if (event.target.files && event.target.files.length > 0) {
      this.solutionForm.patchValue({ attachment: event.target.files[0] });
    }
  }

  submit() {
    if (!this.solutionForm.valid || !this.problemId) return;
    this.loading = true;
    this.error = '';
    this.success = false;
    const formData = new FormData();
  formData.append('ProblemId', this.problemId.toString());
  formData.append('Content', this.solutionForm.value.description);
    if (this.solutionForm.value.attachment) {
      formData.append('Attachment', this.solutionForm.value.attachment);
    }
  this.http.post(`${environment.apiUrl}/solutions`, formData).subscribe({
      next: () => {
        this.success = true;
        this.loading = false;
        setTimeout(() => this.router.navigate(['/problems', this.problemId]), 1200);
      },
      error: (err) => {
        this.error = err?.error?.message || 'Failed to attribute solution.';
        this.loading = false;
      }
    });
  }
}
