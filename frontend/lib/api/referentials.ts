import { api } from "./client";
import type { Referential, ReferentialCategory, ReferentialDetail } from "@/lib/types";

export const referentialsApi = {
  getCategories: () =>
    api.get<ReferentialCategory[]>("/api/referentials/categories"),

  getAll: (params?: { category?: string; search?: string }) => {
    const qs = new URLSearchParams();
    if (params?.category) qs.set("category", params.category);
    if (params?.search) qs.set("search", params.search);
    const query = qs.toString() ? `?${qs}` : "";
    return api.get<Referential[]>(`/api/referentials${query}`);
  },

  getById: (id: string) =>
    api.get<ReferentialDetail>(`/api/referentials/${id}`),

  create: (data: { name: string; slug: string; categoryId: string; version: string; description?: string }) =>
    api.post<Referential>("/api/referentials", data),

  duplicate: (id: string) =>
    api.post<Referential>(`/api/referentials/${id}/duplicate`, {}),

  update: (id: string, data: { name?: string; version?: string; description?: string }) =>
    api.put<Referential>(`/api/referentials/${id}`, data),

  remove: (id: string) =>
    api.delete<void>(`/api/referentials/${id}`),
};
