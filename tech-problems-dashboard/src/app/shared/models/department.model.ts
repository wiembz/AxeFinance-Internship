export interface Department {
  id: number;
  name: string;
  description: string;
  departmentHeadId?: number;
  departmentHeadName?: string;
  createdBy: number;
  createdByName: string;
  createdDate: string;
  lastUpdatedDate?: string;
  projectCount: number;
  userCount: number;
}

export interface CreateDepartmentDto {
  name: string;
  description: string;
  departmentHeadId?: number;
}

export interface UpdateDepartmentDto {
  name: string;
  description: string;
  departmentHeadId?: number;
}

export interface DepartmentListItem {
  id: number;
  name: string;
  description: string;
  departmentHeadName?: string;
  projectCount: number;
  userCount: number;
  createdDate: string;
}

export interface DepartmentDropdownItem {
  id: number;
  name: string;
}

export interface DepartmentProjectsResponse {
  projects: any[]; // You can replace 'any' with your Project interface
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}
