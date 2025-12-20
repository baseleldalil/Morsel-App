export interface ApiContactStatistics {
  totalContacts: number;
  pendingCount: number;
  sendingCount: number;
  sentCount: number;
  deliveredCount: number;
  failedCount: number;
  notValidCount: number;
  hasIssuesCount: number;
  blockedCount: number;
  notInterestedCount: number;
  respondedCount: number;
}

export interface StatusBreakdown {
  status: string;
  count: number;
  percentage: number;
}

export interface StatisticsResponse {
  statistics: ApiContactStatistics;
  status_breakdown: StatusBreakdown[];
}
