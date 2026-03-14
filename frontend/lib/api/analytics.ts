import { fetchWithAuth } from "./client";
import type { DashboardData } from "../types";

export const analyticsApi = {
  getDashboard: () => fetchWithAuth<DashboardData>("/api/analytics/dashboard"),
};
