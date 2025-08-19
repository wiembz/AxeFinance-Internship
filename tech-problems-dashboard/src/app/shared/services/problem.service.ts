import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import {
  PaginatedProblemsResponse,
  Problem,
  ProblemFilters,
  ProblemSubmission,
  ProblemTag
} from '../models/problem.model';

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors?: string[];
}

@Injectable({
  providedIn: 'root'
})
export class ProblemService {
  private apiUrl = `${environment.apiUrl}/problems`;

  constructor(private http: HttpClient) {}

  // Create a new problem
  async createProblem(problemData: ProblemSubmission): Promise<ApiResponse<any>> {
    try {
      const formData = new FormData();
      formData.append('Title', problemData.title);
      formData.append('Description', problemData.description);
      formData.append('ProjectId', problemData.projectId.toString());

      if (problemData.tags && problemData.tags.length > 0) {
        formData.append('Tags', problemData.tags.join(','));
      }

      if (problemData.attachments && problemData.attachments.length > 0) {
        // For now, only send the first attachment since the backend expects single file
        const attachment = problemData.attachments[0];
        if (attachment instanceof File) {
          formData.append('Attachment', attachment);
        }
      }

      const response = await this.http.post<ApiResponse<any>>(
        `${this.apiUrl}`,
        formData
      ).toPromise();

      return response!;
    } catch (error: any) {
      console.error('Error creating problem:', error);
      throw {
        success: false,
        message: error.message || 'Failed to create problem',
        data: null,
        errors: error.errors || []
      };
    }
  }

  // Get problems with pagination and filtering
  getProblems(
    page: number = 1,
    pageSize: number = 10,
    filters?: ProblemFilters
  ): Observable<ApiResponse<PaginatedProblemsResponse>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (filters) {
      if (filters.searchTerm) {
        params = params.set('searchTerm', filters.searchTerm);
      }
      if (filters.departmentId) {
        params = params.set('departmentId', filters.departmentId.toString());
      }
      if (filters.projectId) {
        params = params.set('projectId', filters.projectId.toString());
      }
      if (filters.status) {
        params = params.set('status', filters.status);
      }
      if (filters.assignedTo) {
        params = params.set('assignedTo', filters.assignedTo.toString());
      }
      if (filters.submittedBy) {
        params = params.set('submittedBy', filters.submittedBy.toString());
      }
      if (filters.isActive !== undefined) {
        params = params.set('isActive', filters.isActive.toString());
      }
      if (filters.tags && filters.tags.length > 0) {
        params = params.set('tags', filters.tags.join(','));
      }
      if (filters.dateFrom) {
        params = params.set('dateFrom', filters.dateFrom);
      }
      if (filters.dateTo) {
        params = params.set('dateTo', filters.dateTo);
      }
    }

    return this.http.get<ApiResponse<PaginatedProblemsResponse>>(
      this.apiUrl,
      { params }
    );
  }

  // Get problems for a specific project
  getProjectProblems(
    projectId: number,
    page: number = 1,
    pageSize: number = 10,
    filters?: Omit<ProblemFilters, 'projectId'>
  ): Observable<ApiResponse<PaginatedProblemsResponse>> {
    return this.getProblems(page, pageSize, { ...filters, projectId });
  }

  // Get a single problem by ID
  getProblemById(id: number): Observable<ApiResponse<Problem>> {
    return this.http.get<ApiResponse<Problem>>(`${this.apiUrl}/${id}`);
  }

  // Get popular tags - return observable for async loading
  getPopularTags(limit: number = 10): Observable<ApiResponse<ProblemTag[]>> {
    return this.http.get<ApiResponse<ProblemTag[]>>(
      `${this.apiUrl}/tags/popular`,
      { params: new HttpParams().set('limit', limit.toString()) }
    ).pipe(
      catchError((error: any) => {
        console.warn('Popular tags API not available, using default tags:', error);
        // Return default tags as fallback
        return of({
          success: true,
          message: 'Using default tags',
          data: [
            { id: 1, name: 'bug', count: 0, isActive: true },
            { id: 2, name: 'feature', count: 0, isActive: true },
            { id: 3, name: 'ui', count: 0, isActive: true },
            { id: 4, name: 'performance', count: 0, isActive: true },
            { id: 5, name: 'security', count: 0, isActive: true },
            { id: 6, name: 'database', count: 0, isActive: true },
            { id: 7, name: 'api', count: 0, isActive: true },
            { id: 8, name: 'mobile', count: 0, isActive: true }
          ]
        });
      })
    );
  }
}
