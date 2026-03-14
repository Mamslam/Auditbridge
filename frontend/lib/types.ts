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

// Audit lifecycle: draft → active → submitted → completed → archived
export type AuditStatus = "draft" | "active" | "submitted" | "completed" | "archived";

// Conformity verdict on a question response (aligned with backend)
export type ConformityRating = "compliant" | "minor" | "major" | "critical" | "na" | "pending";

// CAPA lifecycle
export type CapaStatus =
  | "open"
  | "in_progress"
  | "pending_verification"
  | "verified"
  | "cancelled";

// Finding types (severity of the non-conformity)
export type FindingType = "nc_critical" | "nc_major" | "nc_minor" | "observation" | "ofi";
export type FindingStatus = "open" | "acknowledged" | "closed";

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
  categoryId?: string;
  category?: ReferentialCategory;
  organizationId?: string;
  code: string;
  name: string;
  version?: string;
  description?: string;
  isSystem: boolean;
  isPublic: boolean;
  createdAt: string;
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
  orgId: string;
  referentialId: string;
  referentialName: string;
  referentialCode: string;
  title: string;
  description?: string;
  status: AuditStatus;
  clientOrgName?: string;
  clientEmail?: string;
  dueDate?: string;
  scope?: string;
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
  referential: Referential;
  sections: AuditSection[];
  responses: AuditResponse[];
  capas: AuditCapa[];
  findings: AuditFinding[];
  clientToken?: string;
}

export interface AuditResponse {
  id: string;
  auditId: string;
  questionId: string;
  answerValue?: string;
  answerNotes?: string;
  conformity?: ConformityRating;
  auditorComment?: string;
  isFlagged: boolean;
  aiAnalysis?: string;
}

export interface AuditCapa {
  id: string;
  auditId: string;
  findingId?: string;
  responseId?: string;
  questionId?: string;
  title: string;
  description?: string;
  rootCause?: string;
  actionType: string;
  priority: string;
  status: CapaStatus;
  assignedToEmail?: string;
  dueDate?: string;
  completedAt?: string;
  aiGenerated: boolean;
  createdAt?: string;
}

export interface AuditFinding {
  id: string;
  auditId: string;
  questionId?: string;
  responseId?: string;
  findingType: FindingType;
  title: string;
  description?: string;
  observedEvidence?: string;
  regulatoryRef?: string;
  status: FindingStatus;
  capas: AuditCapa[];
  createdAt: string;
  updatedAt: string;
}

export interface AuditEvidence {
  id: string;
  auditId: string;
  findingId?: string;
  responseId?: string;
  capaId?: string;
  fileName: string;
  storagePath: string;
  fileSizeBytes: number;
  mimeType: string;
  description?: string;
  createdAt: string;
}

export interface AuditScore {
  globalScore: number;
  totalQuestions: number;
  totalAnswered: number;
  conformCount: number;
  minorCount: number;
  majorCount: number;
  criticalCount: number;
  naCount: number;
  pendingCount: number;
  sectionScores: SectionScore[];
}

export interface SectionScore {
  sectionId: string;
  title: string;
  conformityPct: number | null;
  conformCount: number;
  minorCount: number;
  majorCount: number;
  criticalCount: number;
  naCount: number;
  totalQuestions: number;
}

export interface AuditReport {
  id: string;
  auditId: string;
  conformityScore?: number;
  totalQuestions?: number;
  conformCount?: number;
  nonConformCount?: number;
  criticalNc: number;
  majorNc: number;
  minorNc: number;
  executiveSummary?: string;
  pdfStoragePath?: string;
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

// ── Analytics ─────────────────────────────────────────────────────────────

export interface OverdueAuditItem {
  id: string;
  title: string;
  status: string;
  dueDate: string;
  daysOverdue: number;
  referentialCode?: string;
}

export interface CapaAgingItem {
  id: string;
  title: string;
  priority: string;
  status: string;
  dueDate?: string;
  daysOverdue?: number;
  auditTitle: string;
}

export interface CapaAgingSummary {
  total: number;
  overdue: number;
  critical: number;
  high: number;
  medium: number;
  low: number;
  overdueItems: CapaAgingItem[];
}

export interface FindingDistribution {
  ncCritical: number;
  ncMajor: number;
  ncMinor: number;
  observation: number;
  ofi: number;
}

export interface MonthlyScorePoint {
  month: string;
  avgScore: number;
  count: number;
}

export interface RepeatFinding {
  title: string;
  count: number;
  auditTitles: string[];
}

export interface DashboardData {
  totalAudits: number;
  active: number;
  submitted: number;
  completed: number;
  overdue: number;
  avgConformityScore?: number;
  overdueAudits: OverdueAuditItem[];
  capaAging: CapaAgingSummary;
  findingDistribution: FindingDistribution;
  conformityTrend: MonthlyScorePoint[];
  repeatFindings: RepeatFinding[];
}

// ── Control Library ───────────────────────────────────────────────────────

export type ControlStatus = "draft" | "active" | "retired";

export interface Control {
  id: string;
  orgId: string;
  code: string;
  title: string;
  description?: string;
  category?: string;
  owner?: string;
  status: ControlStatus;
  mappingCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface ControlMapping {
  id: string;
  controlId: string;
  referentialId: string;
  sectionId?: string;
  questionId?: string;
  notes?: string;
  createdAt: string;
}

export interface ControlDetail extends Omit<Control, "mappingCount"> {
  mappings: ControlMapping[];
}

export interface CoverageControlRef {
  controlId: string;
  code: string;
  title: string;
  status: ControlStatus;
}

export interface QuestionCoverage {
  questionId: string;
  questionCode?: string;
  questionText: string;
  criticality: string;
  sectionId?: string;
  controls: CoverageControlRef[];
}

export interface SectionCoverage {
  sectionId: string;
  sectionTitle: string;
  totalQuestions: number;
  coveredQuestions: number;
  coveragePercent: number;
}

export interface ReferentialCoverage {
  referentialId: string;
  referentialName: string;
  referentialCode: string;
  totalQuestions: number;
  coveredQuestions: number;
  coveragePercent: number;
  sections: SectionCoverage[];
  questions: QuestionCoverage[];
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
