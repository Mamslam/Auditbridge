import { fetchWithAuth } from "./client";
import type { Control, ControlDetail, ControlMapping, ReferentialCoverage } from "../types";

const BASE = "/api/controls";

export const controlsApi = {
  getAll: () => fetchWithAuth<Control[]>(BASE),

  getById: (id: string) => fetchWithAuth<ControlDetail>(`${BASE}/${id}`),

  create: (data: { code: string; title: string; description?: string; category?: string; owner?: string }) =>
    fetchWithAuth<ControlDetail>(BASE, { method: "POST", body: JSON.stringify(data) }),

  update: (id: string, data: { title: string; description?: string; category?: string; owner?: string; status: string }) =>
    fetchWithAuth<ControlDetail>(`${BASE}/${id}`, { method: "PUT", body: JSON.stringify(data) }),

  delete: (id: string) => fetchWithAuth<void>(`${BASE}/${id}`, { method: "DELETE" }),

  addMapping: (id: string, data: { referentialId: string; sectionId?: string; questionId?: string; notes?: string }) =>
    fetchWithAuth<ControlMapping>(`${BASE}/${id}/mappings`, { method: "POST", body: JSON.stringify(data) }),

  removeMapping: (controlId: string, mappingId: string) =>
    fetchWithAuth<void>(`${BASE}/${controlId}/mappings/${mappingId}`, { method: "DELETE" }),

  getCoverage: (referentialId: string) =>
    fetchWithAuth<ReferentialCoverage>(`${BASE}/coverage/${referentialId}`),
};
