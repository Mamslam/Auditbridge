import { ApiResponse, PaginatedResponse } from "@/lib/types";

const API_BASE = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000";

class ApiError extends Error {
  constructor(
    public status: number,
    message: string,
  ) {
    super(message);
    this.name = "ApiError";
  }
}

async function fetchWithAuth<T>(
  path: string,
  options: RequestInit = {},
): Promise<T> {
  const { getToken } = await import("@clerk/nextjs/server").catch(
    () => ({ getToken: async () => null }),
  );

  let token: string | null = null;
  try {
    token = await getToken();
  } catch {
    // client-side: token managed by Clerk header interceptor
  }

  const headers: HeadersInit = {
    "Content-Type": "application/json",
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
    ...options.headers,
  };

  const response = await fetch(`${API_BASE}${path}`, {
    ...options,
    headers,
  });

  if (!response.ok) {
    const errorBody = await response.json().catch(() => ({ message: "Unknown error" }));
    throw new ApiError(response.status, errorBody.message ?? "API error");
  }

  return response.json() as Promise<T>;
}

export const api = {
  get: <T>(path: string) => fetchWithAuth<T>(path),
  post: <T>(path: string, body: unknown) =>
    fetchWithAuth<T>(path, {
      method: "POST",
      body: JSON.stringify(body),
    }),
  put: <T>(path: string, body: unknown) =>
    fetchWithAuth<T>(path, {
      method: "PUT",
      body: JSON.stringify(body),
    }),
  delete: <T>(path: string) => fetchWithAuth<T>(path, { method: "DELETE" }),
};

export type { ApiResponse, PaginatedResponse };
export { ApiError };
