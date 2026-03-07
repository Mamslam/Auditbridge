import { api } from "./client";
import type { Organization, OrganizationType } from "@/lib/types";

export interface CreateOrganizationDto {
  name: string;
  type: OrganizationType;
  countryCode: string;
  language: string;
}

export interface InviteMemberDto {
  email: string;
  role: string;
}

export const organizationsApi = {
  getMe: () => api.get<Organization>("/api/organizations/me"),
  update: (data: Partial<CreateOrganizationDto>) =>
    api.put<Organization>("/api/organizations/me", data),
  create: (data: CreateOrganizationDto) =>
    api.post<Organization>("/api/organizations", data),
  inviteMember: (data: InviteMemberDto) =>
    api.post<void>("/api/organizations/invite", data),
};
