import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface Solution {
  id: number;
  problemId: number;
  userId: number;
  content: string;
  azureDevOpsLink?: string;
  status: string;
  createdDate: string;
  hasAttachment?: boolean;
  attachmentPath?: string;
  userName?: string;
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
}

@Injectable({ providedIn: 'root' })
export class SolutionService {
  private apiUrl = `${environment.apiUrl}/solutions`;

  constructor(private http: HttpClient) {}

  getSolutionsByProblem(problemId: number): Observable<ApiResponse<Solution[]>> {
    return this.http.get<ApiResponse<Solution[]>>(`${this.apiUrl}/problem/${problemId}`);
  }
}
