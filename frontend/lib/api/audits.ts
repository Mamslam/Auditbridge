import { api } from "./client";
import type { Audit, AuditDetail, AuditCapa, AuditReport } from "@/lib/types";

export interface CreateAuditRequest {
  referentialId: string;
  title: string;
  clientOrgName?: string;
  clientEmail?: string;
  deadline?: string;
  scope?: string;
  description?: string;
}

export const auditsApi = {
  getAll: () =>
    api.get<Audit[]>("/api/audits"),

  getById: (id: string) =>
    api.get<AuditDetail>(`/api/audits/${id}`),

  create: (data: CreateAuditRequest) =>
    api.post<Audit>("/api/audits", data),

  activate: (id: string) =>
    api.post<{ clientToken: string; clientTokenExpiresAt: string }>(`/api/audits/${id}/activate`, {}),

  submit: (id: string) =>
    api.post<Audit>(`/api/audits/${id}/submit`, {}),

  complete: (id: string) =>
    api.post<Audit>(`/api/audits/${id}/complete`, {}),

  upsertResponse: (auditId: string, data: { questionId: string; answerValue?: string; answerNotes?: string }) =>
    api.put<void>(`/api/audits/${auditId}/responses`, data),

  setConformity: (auditId: string, responseId: string, data: { conformity: string; auditorComment?: string }) =>
    api.put<void>(`/api/audits/${auditId}/responses/${responseId}/conformity`, data),

  createCapa: (auditId: string, data: { title: string; description?: string; assignedToEmail?: string; dueDate?: string }) =>
    api.post<AuditCapa>(`/api/audits/${auditId}/capas`, data),

  generateReport: (auditId: string) =>
    api.post<AuditReport>(`/api/audits/${auditId}/report`, {}),

  // Client portal (anonymous, token-based)
  getByToken: (token: string) =>
    api.get<AuditDetail>(`/api/audits/portal/${token}`),

  upsertResponseByToken: (token: string, data: { questionId: string; answerValue?: string; answerNotes?: string }) =>
    api.put<void>(`/api/audits/portal/${token}/responses`, { ...data, byClient: true }),

  submitByToken: (token: string) =>
    api.post<void>(`/api/audits/portal/${token}/submit`, {}),
};
