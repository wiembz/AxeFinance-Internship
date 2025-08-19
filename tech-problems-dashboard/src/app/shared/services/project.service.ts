import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { ApiService } from '../../core/api/api.service';
import {
  CreateProjectDto,
  Project,
  ProjectDropdownItem,
  ProjectListItem,
  UpdateProjectDto
} from '../models/project.model';

export interface ProjectListResponse {
  projects: ProjectListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class ProjectService {
  private api = inject(ApiService);

  getProjects(params: {
    page?: number;
    pageSize?: number;
    departmentId?: number;
    searchTerm?: string;
    isActive?: boolean;
    sortBy?: string;
    sortOrder?: string;
  } = {}): Observable<ProjectListResponse> {
    const queryParams: Record<string, string | number> = {
      page: params.page || 1,
      pageSize: params.pageSize || 10,
      ...(params.departmentId && { departmentId: params.departmentId }),
      ...(params.searchTerm && { searchTerm: params.searchTerm }),
      ...(params.sortBy && { sortBy: params.sortBy }),
      ...(params.sortOrder && { sortOrder: params.sortOrder })
    };

    if (params.isActive !== undefined) {
      queryParams['isActive'] = params.isActive ? 'true' : 'false';
    }

    return this.api.get<ProjectListResponse>('/projects', queryParams)
      .pipe(map(response => response.data!));
  }

  getProjectsByDepartment(departmentId: number, params: {
    page?: number;
    pageSize?: number;
  } = {}): Observable<ProjectListResponse> {
    const queryParams: Record<string, string | number> = {
      page: params.page || 1,
      pageSize: params.pageSize || 10
    };

    return this.api.get<ProjectListResponse>(`/projects/department/${departmentId}`, queryParams)
      .pipe(map(response => response.data!));
  }

  getProject(id: number): Observable<Project> {
    return this.api.get<Project>(`/projects/${id}`)
      .pipe(map(response => response.data!));
  }

  createProject(project: CreateProjectDto): Observable<Project> {
    return this.api.post<Project>('/projects', project)
      .pipe(map(response => response.data!));
  }

  updateProject(id: number, project: UpdateProjectDto): Observable<Project> {
    return this.api.put<Project>(`/projects/${id}`, project)
      .pipe(map(response => response.data!));
  }

  deleteProject(id: number): Observable<void> {
    return this.api.delete<void>(`/projects/${id}`)
      .pipe(map(() => void 0));
  }

  getProjectsForDropdown(departmentId: number): Observable<ProjectDropdownItem[]> {
    return this.api.get<ProjectDropdownItem[]>(`/projects/by-department/${departmentId}`)
      .pipe(map(response => response.data!));
  }
}
