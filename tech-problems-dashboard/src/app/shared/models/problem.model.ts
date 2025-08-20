// Main Problem entity as returned by backend (aligned with backend Problem entity and DTOs)
export interface Problem {
  id: number;
  title: string;
  description: string;
  tags: string[]; // UI: array, backend: CSV string
  status: ProblemStatus;
  departmentId: number;
  departmentName?: string;
  projectId: number;
  projectName?: string;
  createdBy: number;
  createdByName?: string;
  assignedToUserId?: number;
  assignedToUserName?: string;
  azureLink?: string;
  attachmentPath?: string;
  likeCount: number;
  isLikedByCurrentUser?: boolean;
  solutionsCount?: number;
  isActive: boolean;
  createdDate: string;
  lastUpdatedDate?: string;
  resolvedDate?: string;
  // For legacy support or future: attachments array
  attachments?: ProblemAttachment[];
}

// Used for creating a new problem (matches backend CreateProblemDto)
export interface ProblemSubmission {
  title: string;
  description: string;
  tags: string[];
  departmentId: number;
  projectId?: number;
  azureLink?: string;
  assignedToUserId?: number;
  attachments: File[];
}

export interface ProblemAttachment {
  id: number;
  fileName: string;
  originalFileName: string;
  fileSize: number;
  contentType: string;
  uploadDate: string;
  uploadedBy: number;
  fileUrl: string;
}

// Remove CreateProblemRequest and FileUploadRequest (not used in current workflow)

// Used for lists/tables (matches backend ProblemResponseDto/ProblemListItem DTO)
export interface ProblemListItem {
  id: number;
  title: string;
  description: string;
  tags: string[];
  status: ProblemStatus;
  departmentId: number;
  departmentName: string;
  projectId: number;
  projectName: string;
  createdBy: number;
  createdByName: string;
  assignedToUserId?: number;
  assignedToUserName?: string;
  azureLink?: string;
  attachmentPath?: string;
  likeCount: number;
  isLikedByCurrentUser?: boolean;
  solutionsCount?: number;
  isActive: boolean;
  createdDate: string;
  lastUpdatedDate?: string;
  resolvedDate?: string;
  attachmentCount: number;
  canEdit: boolean;
  canDelete: boolean;
  priority?: string;
}

// Handles all possible backend pagination response shapes
export interface PaginatedProblemsResponse {
  Problems?: ProblemListItem[];
  problems?: ProblemListItem[];
  items?: ProblemListItem[];
  totalCount: number;
  TotalCount?: number;
  totalPages: number;
  TotalPages?: number;
  currentPage: number;
  Page?: number;
  page?: number;
  pageSize: number;
  PageSize?: number;
}

// Must match backend ProblemStatus enum
export enum ProblemStatus {
  Requested = 'Requested',
  Open = 'Open',
  InProgress = 'InProgress',
  UnderReview = 'UnderReview',
  Resolved = 'Resolved',
  Closed = 'Closed',
  Rejected = 'Rejected'
}

export interface ProblemTag {
  id: number;
  name: string;
  color?: string;
  isActive: boolean;
}

export interface ProblemFilters {
  searchTerm?: string;
  departmentId?: number;
  projectId?: number;
  status?: ProblemStatus;
  assignedTo?: number;
  submittedBy?: number;
  tags?: string[];
  isActive?: boolean;
  dateFrom?: string;
  dateTo?: string;
}
