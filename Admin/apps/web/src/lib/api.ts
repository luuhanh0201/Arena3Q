import type { ApiResponse } from "@arena3q/contracts";
export class ApiFailure extends Error {
  constructor(
    readonly status: number,
    message: string,
    readonly requestId?: string,
  ) {
    super(message);
  }
}
export async function api<T>(
  path: string,
  options: RequestInit = {},
): Promise<ApiResponse<T>> {
  let response: Response;
  try {
    response = await fetch(`/api/admin/v1${path}`, {
      credentials: "same-origin",
      ...options,
      headers: {
        ...(options.body ? { "Content-Type": "application/json" } : {}),
        ...options.headers,
      },
    });
  } catch {
    throw new ApiFailure(0, "Không thể kết nối API. Vui lòng thử lại.");
  }
  let body;
  try {
    body = await response.json();
  } catch {
    throw new ApiFailure(response.status, "API trả về dữ liệu không hợp lệ.");
  }
  if (!response.ok || body.error)
    throw new ApiFailure(
      response.status,
      body.error?.message ?? "Không thể tải dữ liệu.",
      body.error?.requestId,
    );
  return body as ApiResponse<T>;
}
