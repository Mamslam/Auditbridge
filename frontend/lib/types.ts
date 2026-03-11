export type OrganizationType = "auditor" | "client";

export type UserRole =
  | "auditor_lead"
  | "auditor_junior"
  | "auditor_viewer"
  | "client_admin"
  | "client_contributor"
  | "client_viewer"
  | "platform_admin";

export type AnswerType =
  | "yes_no"
  | "rating_1_5"
  | "text"
  | "file_upload"
  | "multi_select"
  | "numeric";

export type Criticality = "info" | "minor" | "major" | "critical";
export type AuditStatus = "draft" | "active" | "submitted" | "completed" | "archived";
export type ConformityRating = "compliant" | "minor" | "major" | "critical" | "na";
export type CapaStatus =
  | "open"
  | "in_progress"
  | "pending_verification"
  | "verified"
  | "cancelled";

// ── Referential ───────────────────────────────────────────────────────────

export interface ReferentialCategory {
  id: string;
  slug: string;
  label: string;
  color: string;
  icon: string;
}

export interface Referential {
  id: string;
  categoryId: string;
  category?: ReferentialCategory;
  organizationId?: string;
  slug: string;
  name: string;
  version: string;
  description?: string;
  language: string;
  isSystem: boolean;
  isPublic: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface ReferentialDetail extends Referential {
  sections: TemplateSection[];
}

export interface TemplateSection {
  id: string;
  referentialId: string;
  title: string;
  description?: string;
  frameworkRef?: string;
  orderIndex: number;
  questions?: TemplateQuestion[];
}

export interface TemplateQuestion {
  id: string;
  sectionId: string;
  text: string;
  guidance?: string;
  regulatoryRef?: string;
  answerType: AnswerType;
  criticality: Criticality;
  isMandatory: boolean;
  expectedEvidence: string[];
  tags: string[];
  orderIndex: number;
}

// ── Audit ─────────────────────────────────────────────────────────────────

export interface Audit {
  id: string;
  organizationId: string;
  referentialId: string;
  referential?: Referential;
  clientOrgId?: string;
  title: string;
  status: AuditStatus;
  scheduledDate?: string;
  deadline?: string;
  scope?: string;
  complianceScore?: number;
  clientToken?: string;
  createdAt: string;
  updatedAt: string;
}

export interface AuditSection {
  id: string;
  title: string;
  orderIndex: number;
  questions: AuditQuestion[];
}

export interface AuditQuestion {
  id: string;
  code: string;
  text: string;
  guidance?: string;
  answerType: AnswerType;
  isMandatory: boolean;
  criticality: Criticality;
}

export interface AuditDetail extends Audit {
  sections: AuditSection[];
  responses: AuditResponse[];
  capas: AuditCapa[];
}

export interface AuditResponse {
  id: string;
  auditId: string;
  questionId: string;
  question?: TemplateQuestion;
  answerValue?: string;
  conformity?: ConformityRating;
  comment?: string;
  aiAnalysis?: string;
  isFlagged: boolean;
  updatedAt: string;
}

export interface AuditCapa {
  id: string;
  auditId: string;
  responseId?: string;
  title: string;
  description?: string;
  status: CapaStatus;
  assignedTo?: string;
  dueDate?: string;
  completedAt?: string;
  verifiedAt?: string;
  aiGenerated: boolean;
  createdAt: string;
}

export interface AuditReport {
  id: string;
  auditId: string;
  narrative?: string;
  pdfUrl?: string;
  generatedAt: string;
}

// ── Organization / User ───────────────────────────────────────────────────

export interface Organization {
  id: string;
  name: string;
  type: OrganizationType;
  plan: string;
  countryCode: string;
  language: string;
  logoUrl?: string;
  isActive: boolean;
  createdAt: string;
}

export interface User {
  id: string;
  clerkId: string;
  organizationId: string;
  email: string;
  fullName?: string;
  role: UserRole;
  isActive: boolean;
  lastLoginAt?: string;
  createdAt: string;
}

// ── API helpers ───────────────────────────────────────────────────────────

export interface ApiResponse<T> {
  data: T;
  message?: string;
}

export interface PaginatedResponse<T> {
  data: T[];
  nextCursor?: string;
  hasMore: boolean;
  total: number;
}
