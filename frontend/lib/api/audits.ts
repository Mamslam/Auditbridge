import { api } from "./client";
import type {
  Audit, AuditDetail, AuditCapa, AuditFinding, AuditEvidence,
  AuditScore, AuditReport, ConformityRating
} from "@/lib/types";

export interface CreateAuditRequest {
  referentialId: string;
  title: string;
  clientOrgName?: string;
  clientEmail?: string;
  dueDate?: string;
  scope?: string;
  description?: string;
}

export interface CreateFindingRequest {
  findingType: string;
  title: string;
  questionId?: string;
  responseId?: string;
  description?: string;
  observedEvidence?: string;
  regulatoryRef?: string;
}

export interface CreateCapaRequest {
  title: string;
  findingId?: string;
  questionId?: string;
  responseId?: string;
  priority?: string;
  actionType?: string;
  description?: string;
  assignedToEmail?: string;
  dueDate?: string;
}

export interface RegisterEvidenceRequest {
  fileName: string;
  storagePath: string;
  fileSizeBytes: number;
  mimeType: string;
  findingId?: string;
  responseId?: string;
  capaId?: string;
  description?: string;
}

export const auditsApi = {
  // ── Audits ────────────────────────────────────────────────────────────
  getAll: () =>
    api.get<Audit[]>("/api/audits"),

  getById: (id: string) =>
    api.get<AuditDetail>(`/api/audits/${id}`),

  create: (data: CreateAuditRequest) =>
    api.post<Audit>("/api/audits", data),

  update: (id: string, data: { title: string; description?: string; scope?: string; dueDate?: string }) =>
    api.put<Audit>(`/api/audits/${id}`, data),

  // ── State transitions ─────────────────────────────────────────────────
  activate: (id: string) =>
    api.post<{ clientToken: string; clientTokenExpiresAt: string }>(`/api/audits/${id}/activate`, {}),

  submit: (id: string) =>
    api.post<Audit>(`/api/audits/${id}/submit`, {}),

  complete: (id: string) =>
    api.post<Audit>(`/api/audits/${id}/complete`, {}),

  forceClose: (id: string) =>
    api.post<Audit>(`/api/audits/${id}/force-close`, {}),

  refreshToken: (id: string) =>
    api.post<{ clientToken: string; clientTokenExpiresAt: string }>(`/api/audits/${id}/refresh-token`, {}),

  // ── Score ─────────────────────────────────────────────────────────────
  getScore: (id: string) =>
    api.get<AuditScore>(`/api/audits/${id}/score`),

  // ── Responses ─────────────────────────────────────────────────────────
  upsertResponse: (auditId: string, data: { questionId: string; answerValue?: string; answerNotes?: string }) =>
    api.put<void>(`/api/audits/${auditId}/responses`, data),

  setConformity: (auditId: string, responseId: string, data: { conformity: ConformityRating; auditorComment?: string }) =>
    api.put<void>(`/api/audits/${auditId}/responses/${responseId}/conformity`, data),

  flagResponse: (auditId: string, responseId: string, flagged: boolean) =>
    api.put<void>(`/api/audits/${auditId}/responses/${responseId}/flag`, { flagged }),

  // ── CAPAs ─────────────────────────────────────────────────────────────
  createCapa: (auditId: string, data: CreateCapaRequest) =>
    api.post<AuditCapa>(`/api/audits/${auditId}/capas`, data),

  updateCapa: (auditId: string, capaId: string, data: {
    title: string; actionType: string; priority: string;
    description?: string; rootCause?: string; assignedToEmail?: string; dueDate?: string;
  }) =>
    api.put<AuditCapa>(`/api/audits/${auditId}/capas/${capaId}`, data),

  updateCapaStatus: (auditId: string, capaId: string, status: string) =>
    api.post<AuditCapa>(`/api/audits/${auditId}/capas/${capaId}/status`, { status }),

  // ── Findings ──────────────────────────────────────────────────────────
  getFindings: (auditId: string) =>
    api.get<AuditFinding[]>(`/api/audits/${auditId}/findings`),

  createFinding: (auditId: string, data: CreateFindingRequest) =>
    api.post<AuditFinding>(`/api/audits/${auditId}/findings`, data),

  updateFinding: (auditId: string, findingId: string, data: {
    findingType: string; title: string;
    description?: string; observedEvidence?: string; regulatoryRef?: string;
  }) =>
    api.put<AuditFinding>(`/api/audits/${auditId}/findings/${findingId}`, data),

  acknowledgeFinding: (auditId: string, findingId: string) =>
    api.post<AuditFinding>(`/api/audits/${auditId}/findings/${findingId}/acknowledge`, {}),

  closeFinding: (auditId: string, findingId: string) =>
    api.post<AuditFinding>(`/api/audits/${auditId}/findings/${findingId}/close`, {}),

  createCapaFromFinding: (auditId: string, findingId: string, data: CreateCapaRequest) =>
    api.post<AuditCapa>(`/api/audits/${auditId}/findings/${findingId}/capas`, data),

  deleteFinding: (auditId: string, findingId: string) =>
    api.delete<void>(`/api/audits/${auditId}/findings/${findingId}`),

  // ── Evidence ──────────────────────────────────────────────────────────
  getEvidence: (auditId: string) =>
    api.get<AuditEvidence[]>(`/api/audits/${auditId}/evidence`),

  presignUpload: (auditId: string, fileName: string) =>
    api.post<{ signedUrl: string; storagePath: string; expiresAt: string }>(
      `/api/audits/${auditId}/evidence/presign?fileName=${encodeURIComponent(fileName)}`, {}),

  registerEvidence: (auditId: string, data: RegisterEvidenceRequest) =>
    api.post<AuditEvidence>(`/api/audits/${auditId}/evidence`, data),

  getDownloadUrl: (auditId: string, evidenceId: string) =>
    api.get<{ url: string }>(`/api/audits/${auditId}/evidence/${evidenceId}/download`),

  deleteEvidence: (auditId: string, evidenceId: string) =>
    api.delete<void>(`/api/audits/${auditId}/evidence/${evidenceId}`),

  // ── Report ────────────────────────────────────────────────────────────
  generateReport: (auditId: string, executiveSummary?: string) =>
    // Returns a PDF blob
    fetch(`${process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5050"}/api/audits/${auditId}/report`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ executiveSummary }),
    }),

  getReport: (auditId: string) =>
    api.get<AuditReport>(`/api/audits/${auditId}/report`),

  // ── Client portal (anonymous, token-based) ────────────────────────────
  getByToken: (token: string) =>
    api.get<AuditDetail>(`/api/audits/portal/${token}`),

  upsertResponseByToken: (token: string, data: { questionId: string; answerValue?: string; answerNotes?: string }) =>
    api.put<void>(`/api/audits/portal/${token}/responses`, { ...data, byClient: true }),

  submitByToken: (token: string) =>
    api.post<void>(`/api/audits/portal/${token}/submit`, {}),
};
