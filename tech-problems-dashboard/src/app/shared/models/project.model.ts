export interface Project {
  id: number;
  name: string;
  description: string;
  departmentId: number;
  departmentName: string;
  createdBy: string;
  createdDate: string;
  isActive: boolean;
  canEdit: boolean;
  canDelete: boolean;
  problems?: ProjectProblemSummary[];
}

export interface CreateProjectDto {
  name: string;
  description: string;
  departmentId: number;
}

export interface UpdateProjectDto {
  name: string;
  description: string;
}

export interface ProjectListItem {
  id: number;
  name: string;
  description: string;
  departmentId: number;
  departmentName: string;
  createdBy: string;
  createdDate: string;
  problemsCount: number;
  isActive: boolean;
  canEdit: boolean;
  canDelete: boolean;
}

export interface ProjectDropdownItem {
  id: number;
  name: string;
  departmentId: number;
}

export interface ProjectProblemSummary {
  id: number;
  title: string;
  description: string;
  tags: string;
  status: string;
  priority: number;
  createdDate: string;
  createdBy: string;
  solutionsCount: number;
  likesCount: number;
  hasAttachment: boolean;
}
