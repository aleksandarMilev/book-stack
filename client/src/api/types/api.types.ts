export interface PaginatedResponse<TItem> {
  items: TItem[];
  totalItems: number;
  pageIndex: number;
  pageSize: number;
}

export interface ApiErrorPayload {
  errorMessage?: string;
  title?: string;
  errors?: Record<string, string[]>;
}
