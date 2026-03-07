export type OrganizationType = "auditor" | "client";

export type UserRole =
  | "auditor_lead"
  | "auditor_junior"
  | "auditor_viewer"
  | "client_admin"
  | "client_contributor"
  | "client_viewer"
  | "platform_admin";

export type AuditFramework =
  | "GMP"
  | "EU_GMP"
  | "ISO_9001"
  | "ISO_27001"
  | "ISO_14001"
  | "NIS2"
  | "RGPD"
  | "HACCP"
  | "CSRD"
  | "DORA"
  | "CUSTOM";

export type CampaignStatus =
  | "draft"
  | "sent"
  | "in_progress"
  | "client_submitted"
  | "under_review"
  | "report_generated"
  | "closed";

export type CapaStatus =
  | "open"
  | "in_progress"
  | "pending_verification"
  | "closed"
  | "overdue";

export type CapaSeverity = "minor" | "major" | "critical";

export type AuditorRating =
  | "compliant"
  | "minor"
  | "major"
  | "critical"
  | "na";

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

export interface AuditTemplate {
  id: string;
  organizationId: string;
  name: string;
  framework: AuditFramework;
  version: string;
  language: string;
  description?: string;
  isPublic: boolean;
  sections: TemplateSection[];
  createdBy: string;
  createdAt: string;
  updatedAt: string;
}

export interface TemplateSection {
  id: string;
  templateId: string;
  title: string;
  description?: string;
  orderIndex: number;
  frameworkReference?: string;
  isMandatory: boolean;
  questions?: TemplateQuestion[];
}

export interface TemplateQuestion {
  id: string;
  sectionId: string;
  text: string;
  type: "yes_no" | "rating" | "text" | "file_upload" | "multi_select";
  isMandatory: boolean;
  weight: number;
  gmpCritical: boolean;
  regulatoryReference?: string;
  guidance?: string;
  orderIndex: number;
}

export interface AuditCampaign {
  id: string;
  templateId: string;
  auditorOrgId: string;
  clientOrgId: string;
  leadAuditorId: string;
  title: string;
  status: CampaignStatus;
  scheduledDate?: string;
  deadline?: string;
  scope?: string;
  complianceScore?: number;
  createdAt: string;
  updatedAt: string;
}

export interface CapaAction {
  id: string;
  campaignId: string;
  responseId?: string;
  title: string;
  description?: string;
  severity: CapaSeverity;
  status: CapaStatus;
  assignedTo?: string;
  dueDate?: string;
  closedAt?: string;
  closingEvidence?: string;
  aiGenerated: boolean;
  createdAt: string;
}

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
