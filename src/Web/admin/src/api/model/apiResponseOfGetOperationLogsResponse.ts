import type { GetOperationLogsResponse } from './getOperationLogsResponse';

export interface ApiResponseOfGetOperationLogsResponse {
  code?: number;
  message?: string;
  data?: GetOperationLogsResponse | null;
}
