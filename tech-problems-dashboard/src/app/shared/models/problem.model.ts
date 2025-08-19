export interface Problem {
  id: number;
  title: string;
  description: string;
  tags: string[];
  status: ProblemStatus;
  departmentId: number;
  departmentName?: string;
  projectId: number;
  projectName?: string;
  submittedBy: number;
  submittedByName?: string;
  assignedTo?: number;
  assignedToName?: string;
  createdDate: string;
  lastUpdatedDate?: string;
  resolvedDate?: string;
  attachments: ProblemAttachment[];
  isActive: boolean;
}

export interface ProblemSubmission {
  title: string;
  description: string;
  tags: string[];
  departmentId: number;
  projectId: number;
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

export interface CreateProblemRequest {
  title: string;
  description: string;
  tags: string[];
  departmentId: number;
  projectId: number;
  attachments?: FileUploadRequest[];
}

export interface FileUploadRequest {
  fileName: string;
  fileContent: string; // Base64 encoded
  contentType: string;
}

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
  submittedBy: number;
  submittedByName: string;
  assignedTo?: number;
  assignedToName?: string;
  createdDate: string;
  lastUpdatedDate?: string;
  attachmentCount: number;
  isActive: boolean;
  canEdit: boolean;
  canDelete: boolean;
}

export interface PaginatedProblemsResponse {
  Problems?: ProblemListItem[]; // Backend uses 'Problems'
  items?: ProblemListItem[];    // Fallback for other APIs that might use 'items'
  totalCount: number;
  TotalCount?: number; // Backend uses 'TotalCount'
  totalPages: number;
  TotalPages?: number; // Backend uses 'TotalPages'
  currentPage: number;
  Page?: number;       // Backend uses 'Page'
  pageSize: number;
  PageSize?: number;   // Backend uses 'PageSize'
}

export enum ProblemStatus {
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
