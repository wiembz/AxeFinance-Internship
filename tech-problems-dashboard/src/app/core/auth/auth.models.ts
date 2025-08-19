export type UserRole = 'SuperAdmin' | 'Admin' | 'Contributor' | 'Viewer';

export interface User {
  id: number;
  username: string;
  email: string;
  role: UserRole;
  departmentName?: string;
  isActive: boolean;
  createdDate: string;
  lastLoginDate?: string;
}

export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message: string;
  errors?: string[];
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
  departmentId?: number;
  projectId?: number;
}
