import type { OperationLogDto } from './operationLogDto';

export interface GetOperationLogsResponse {
  total?: number;
  items?: OperationLogDto[] | null;
  page?: number;
  pageSize?: number;
}
