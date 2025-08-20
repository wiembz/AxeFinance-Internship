import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { ApiService } from '../../core/api/api.service';
import {
  CreateDepartmentDto,
  Department,
  DepartmentDropdownItem,
  DepartmentListItem,
  UpdateDepartmentDto
} from '../models/department.model';

export interface DepartmentListResponse {
  departments: DepartmentListItem[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface DepartmentProjectsResponse {
  projects: any[]; // You can replace 'any' with your Project interface
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

@Injectable({
  providedIn: 'root'
})
export class DepartmentService {
  private api = inject(ApiService);

  getDepartments(params: {
    page?: number;
    pageSize?: number;
    search?: string;
  } = {}): Observable<DepartmentListResponse> {
    const queryParams: Record<string, string | number> = {
      page: params.page || 1,
      pageSize: params.pageSize || 10,
      ...(params.search && { searchTerm: params.search })
    };

    return this.api.get<DepartmentListResponse>('/departments', queryParams)
      .pipe(map(response => response.data!));
  }

  getDepartment(id: number): Observable<Department> {
    return this.api.get<Department>(`/departments/${id}`)
      .pipe(map(response => response.data!));
  }

  getProjectsByDepartment(departmentId: number, params: {
    page?: number;
    pageSize?: number;
    search?: string;
  } = {}): Observable<DepartmentProjectsResponse> {
    const queryParams: Record<string, string | number> = {
      page: params.page || 1,
      pageSize: params.pageSize || 10,
      ...(params.search && { searchTerm: params.search })
    };

    return this.api.get<DepartmentProjectsResponse>(`/departments/${departmentId}/projects`, queryParams)
      .pipe(map(response => response.data!));
  }

  createDepartment(department: CreateDepartmentDto): Observable<Department> {
    return this.api.post<Department>('/departments', department)
      .pipe(map(response => response.data!));
  }

  updateDepartment(id: number, department: UpdateDepartmentDto): Observable<Department> {
    return this.api.put<Department>(`/departments/${id}`, department)
      .pipe(map(response => response.data!));
  }

  deleteDepartment(id: number): Observable<void> {
    return this.api.delete<void>(`/departments/${id}`)
      .pipe(map(() => void 0));
  }

  getDepartmentsForDropdown(): Observable<DepartmentDropdownItem[]> {
    return this.api.get<DepartmentDropdownItem[]>('/projects/department-dropdown')
      .pipe(map(response => response.data!));
  }
}
