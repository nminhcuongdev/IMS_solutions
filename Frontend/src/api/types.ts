
export interface ApiSuccess<T> {
  success: true;
  data: T;
}

export interface ApiListSuccess<T> {
  success: true;
  data: T[];
  meta: PageMeta;
}

export interface PageMeta {
  total: number;
  page: number;
  pageSize: number;
}

export interface ApiErrorBody {
  code: string;
  httpStatus: number;
  severity: string;
  message: string;
  args?: string[] | null;
  messageKey?: string | null;
}

export interface ApiError {
  success: false;
  error: ApiErrorBody;
}

export class ApiCallError extends Error {
  code: string;
  httpStatus: number;
  args: string[];
  messageKey: string | null;
  constructor(body: ApiErrorBody) {
    super(body.message);
    this.code = body.code;
    this.httpStatus = body.httpStatus;
    this.args = body.args ?? [];
    this.messageKey = body.messageKey ?? null;
  }
}
