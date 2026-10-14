import axios from "axios";

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL || "/api",
  headers: {
    "Content-Type": "application/json",
  },
  timeout: 30000,
});

apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem("sentinelops_token");
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  const orgId = localStorage.getItem("sentinelops_active_org_id");
  if (orgId) {
    config.headers["X-Organization-Id"] = orgId;
  }
  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401 && !window.location.pathname.startsWith("/login") && !window.location.pathname.startsWith("/register")) {
      localStorage.removeItem("sentinelops_token");
      localStorage.removeItem("sentinelops_refresh_token");
      window.location.href = "/login";
    }
    return Promise.reject(error);
  }
);

export default apiClient;

// API types
export interface ApiResponse<T> {
  success: boolean;
  data: T;
  message: string | null;
  errors: string[];
}

export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface DashboardSummary {
  totalApplications: number;
  totalServices: number;
  activeAlerts: number;
  healthyServices: number;
  errorRate: number;
  avgResponseTimeMs: number;
  totalLogsToday: number;
  totalTracesToday: number;
}

export interface ApplicationDto {
  id: string;
  name: string;
  displayName: string;
  description: string;
  isActive: boolean;
  createdAtUtc: string;
  environments?: EnvironmentDto[];
  services?: ServiceDto[];
}

export interface EnvironmentDto {
  id: string;
  applicationId: string;
  name: string;
  environmentType: string;
  isActive: boolean;
  createdAtUtc: string;
}

export interface ServiceDto {
  id: string;
  applicationId: string;
  name: string;
  displayName: string;
  isActive: boolean;
  createdAtUtc: string;
}

export interface AuthResponse {
  userId: string;
  token: string;
  refreshToken: string;
  expiration: string;
  userName: string;
  email: string;
  fullName: string;
  roles: string[];
}

export interface UserProfileDto {
  id: string;
  userName: string;
  email: string;
  fullName: string;
  isActive: boolean;
  createdAtUtc: string;
  roles: string[];
}

export interface ApiKeyDto {
  id: string;
  applicationId: string;
  name: string;
  keyPrefix: string;
  createdAtUtc: string;
  expiresAtUtc: string | null;
  revokedAtUtc: string | null;
  lastUsedAtUtc: string | null;
  createdBy: string;
  isActive: boolean;
}

export interface ApiKeyCreatedResponse {
  id: string;
  applicationId: string;
  name: string;
  keyPrefix: string;
  rawApiKey: string;
  createdAtUtc: string;
  expiresAtUtc: string | null;
}

export interface HealthCheckDto {
  status: string;
  totalDuration: string;
  entries: Record<string, { status: string; description: string }>;
}

export interface LogEntryDto {
  id: string;
  applicationId: string;
  serviceName: string;
  environmentName: string;
  severity: string;
  message: string;
  messageTemplate?: string;
  properties?: string;
  exceptionDetail?: string;
  traceId?: string;
  spanId?: string;
  correlationId?: string;
  timestamp: string;
}

export interface LogMetadataDto {
  services: string[];
  environments: string[];
}

export interface TraceDto {
  traceId: string;
  applicationId: string;
  serviceName: string;
  environmentName: string;
  rootSpanName: string;
  durationMs: number;
  spanCount: number;
  hasErrors: boolean;
  startedAtUtc: string;
}

export interface SpanDetailDto {
  id: string;
  spanId: string;
  traceId: string;
  parentSpanId?: string | null;
  operationName: string;
  serviceName: string;
  durationMs: number;
  status: string;
  startedAtUtc: string;
  attributes?: string;
  depth: number;
  offsetMs: number;
  offsetPercent: number;
  durationPercent: number;
}

export interface TraceDetailDto {
  trace: TraceDto;
  spans: SpanDetailDto[];
  correlatedLogs: LogEntryDto[];
  services: string[];
  maxDepth: number;
  totalDurationMs: number;
}

export interface MetricTimeSeriesPointDto {
  timestamp: string;
  requestsPerSecond: number;
  errorRatePercent: number;
  avgDurationMs: number;
  p50DurationMs: number;
  p95DurationMs: number;
  p99DurationMs: number;
  totalRequests: number;
  errorRequests: number;
}

export interface MetricSeriesResponse {
  points: MetricTimeSeriesPointDto[];
  avgLatencyMs: number;
  p95LatencyMs: number;
  overallErrorRate: number;
  totalThroughputRps: number;
  totalRequests: number;
}

export interface ExceptionGroupDto {
  fingerprint: string;
  exceptionType: string;
  message: string;
  serviceName: string;
  environmentName: string;
  count: number;
  firstSeenAtUtc: string;
  lastSeenAtUtc: string;
  sampleTraceId?: string | null;
  sampleLogId: string;
}

export interface ServiceHealthSummaryDto {
  applicationId: string;
  applicationName: string;
  serviceName: string;
  environmentName: string;
  status: "Healthy" | "Degraded" | "Unhealthy" | string;
  totalRequests: number;
  totalErrors: number;
  errorRatePercent: number;
  avgDurationMs: number;
  p95DurationMs: number;
  lastSeenAtUtc?: string | null;
}

// Alerting types
export type AlertConditionType =
  | "ErrorRateAboveThreshold"
  | "P95LatencyAboveThreshold"
  | "P99LatencyAboveThreshold"
  | "ThroughputBelowThreshold"
  | "ThroughputAboveThreshold"
  | "ExceptionCountAboveThreshold"
  | "LogSeverityCountAboveThreshold"
  | "NoTelemetryReceived";

export type ComparisonOperator =
  | "GreaterThan"
  | "GreaterThanOrEqual"
  | "LessThan"
  | "LessThanOrEqual"
  | "Equal"
  | "NotEqual";

export type AlertStatus = "Normal" | "Pending" | "Firing" | "Acknowledged" | "Resolved" | "Suppressed";
export type AlertSeverity = "Trace" | "Debug" | "Information" | "Warning" | "Error" | "Critical";

export interface AlertRuleDto {
  id: string;
  name: string;
  description: string;
  applicationId: string;
  environmentName?: string | null;
  serviceName?: string | null;
  conditionType: AlertConditionType | string;
  operator: ComparisonOperator | string;
  condition: string;
  metricName: string;
  threshold: number;
  evaluationWindowMinutes: number;
  evaluationFrequencyMinutes: number;
  consecutiveCountRequired: number;
  currentConsecutiveFailures: number;
  cooldownMinutes: number;
  status: AlertStatus;
  severity: AlertSeverity;
  isEnabled: boolean;
  lastEvaluatedAtUtc?: string | null;
  lastFiredAtUtc?: string | null;
  lastResolvedAtUtc?: string | null;
  lastMeasuredValue?: number | null;
}

export interface AlertEventDto {
  id: string;
  alertRuleId: string;
  alertRuleName: string;
  severity: AlertSeverity;
  oldStatus: AlertStatus;
  newStatus: AlertStatus;
  measuredValue: number;
  threshold: number;
  triggeredAtUtc: string;
  message: string;
}

export interface AlertSummaryDto {
  totalRules: number;
  enabledRules: number;
  firingAlerts: number;
  acknowledgedAlerts: number;
  pendingAlerts: number;
  criticalAlerts: number;
  warningAlerts: number;
}

export interface CreateAlertRuleRequest {
  name: string;
  description?: string;
  applicationId: string;
  environmentName?: string | null;
  serviceName?: string | null;
  conditionType: string;
  operator: string;
  metricName?: string;
  threshold: number;
  evaluationWindowMinutes: number;
  evaluationFrequencyMinutes?: number;
  consecutiveCountRequired: number;
  cooldownMinutes: number;
  severity: string;
  notificationChannelIds?: string[];
}

export interface UpdateAlertRuleRequest {
  name: string;
  description?: string;
  environmentName?: string | null;
  serviceName?: string | null;
  conditionType: string;
  operator: string;
  metricName?: string;
  threshold: number;
  evaluationWindowMinutes: number;
  evaluationFrequencyMinutes?: number;
  consecutiveCountRequired: number;
  cooldownMinutes: number;
  severity: string;
  notificationChannelIds?: string[];
}

// Incident types
export type IncidentStatus = "Open" | "Investigating" | "Mitigated" | "Resolved" | "Closed";
export type IncidentTimelineEventType =
  | "Created"
  | "StatusChanged"
  | "SeverityChanged"
  | "Assigned"
  | "CommentAdded"
  | "AlertLinked"
  | "TraceLinked"
  | "Mitigated"
  | "Resolved"
  | "Closed";

export interface IncidentDto {
  id: string;
  incidentNumber: string;
  title: string;
  description: string;
  severity: AlertSeverity;
  status: IncidentStatus;
  applicationId: string;
  environmentName?: string | null;
  serviceName?: string | null;
  assignedToUserId?: string | null;
  assignedToUserName?: string | null;
  createdAtUtc: string;
  acknowledgedAtUtc?: string | null;
  acknowledgedBy?: string | null;
  mitigatedAtUtc?: string | null;
  mitigatedBy?: string | null;
  resolvedAtUtc?: string | null;
  resolvedBy?: string | null;
  closedAtUtc?: string | null;
  closedBy?: string | null;
  rootCauseSummary?: string | null;
  resolutionNotes?: string | null;
  relatedTraceId?: string | null;
  timelineEventsCount: number;
  commentsCount: number;
}

export interface IncidentTimelineEventDto {
  id: string;
  eventType: string;
  timestampUtc: string;
  actorId: string;
  actorName: string;
  title: string;
  description: string;
}

export interface IncidentCommentDto {
  id: string;
  userId: string;
  userName: string;
  commentText: string;
  createdAtUtc: string;
}

export interface IncidentDetailDto extends IncidentDto {
  timelineEvents: IncidentTimelineEventDto[];
  comments: IncidentCommentDto[];
  linkedAlerts: AlertRuleDto[];
}

export interface CreateIncidentRequest {
  title: string;
  description: string;
  severity: string;
  applicationId: string;
  environmentName?: string | null;
  serviceName?: string | null;
  assignedToUserId?: string | null;
  relatedTraceId?: string | null;
  alertRuleIds?: string[];
}

export interface UpdateIncidentRequest {
  title: string;
  description: string;
  severity: string;
  environmentName?: string | null;
  serviceName?: string | null;
  rootCauseSummary?: string | null;
  resolutionNotes?: string | null;
  relatedTraceId?: string | null;
}

export interface TransitionIncidentStatusRequest {
  status: IncidentStatus;
  note?: string;
  resolutionNotes?: string;
  rootCauseSummary?: string;
}

export interface AssignIncidentRequest {
  assignedToUserId?: string | null;
  notes?: string;
}

export interface AddIncidentCommentRequest {
  commentText: string;
}

// Notification Channels & InApp
export type NotificationChannelType = "Email" | "Webhook" | "Slack" | "PagerDuty";

export interface NotificationChannelDto {
  id: string;
  name: string;
  description: string;
  type: NotificationChannelType | string;
  destination: string;
  isEnabled: boolean;
  applicationId?: string | null;
  createdBy?: string | null;
  createdAtUtc: string;
}

export interface CreateNotificationChannelRequest {
  name: string;
  description?: string;
  type: string;
  destination: string;
  webhookSecret?: string;
  applicationId?: string | null;
}

export interface InAppNotificationDto {
  id: string;
  title: string;
  message: string;
  severity: AlertSeverity;
  linkUrl?: string | null;
  isRead: boolean;
  readAtUtc?: string | null;
  createdAtUtc: string;
}

// SLO types
export type SloIndicatorType = "Availability" | "Latency";
export type SloEvaluationWindow = "Rolling24Hours" | "Rolling7Days" | "Rolling30Days";

export interface SloDto {
  id: string;
  name: string;
  description: string;
  applicationId: string;
  serviceName: string;
  environmentName?: string | null;
  indicatorType: SloIndicatorType | string;
  targetPercentage: number;
  evaluationWindow: SloEvaluationWindow | string;
  latencyThresholdMs?: number | null;
  isEnabled: boolean;
  currentCompliancePercentage?: number | null;
  currentErrorBudgetRemaining?: number | null;
  burnRate?: number | null;
  isBreached: boolean;
  lastEvaluatedAtUtc?: string | null;
  createdAtUtc: string;
}

export interface SloEvaluationPointDto {
  timestamp: string;
  compliancePercentage: number;
  errorBudgetRemaining: number;
  burnRate: number;
  totalRequests: number;
  goodRequests: number;
  isBreached: boolean;
}

export interface SloComplianceHistoryDto {
  serviceLevelObjectiveId: string;
  name: string;
  targetPercentage: number;
  currentCompliance: number;
  errorBudgetRemaining: number;
  burnRate: number;
  isBreached: boolean;
  history: SloEvaluationPointDto[];
}

export interface CreateSloRequest {
  name: string;
  description?: string;
  applicationId: string;
  serviceName: string;
  environmentName?: string | null;
  indicatorType: string;
  targetPercentage: number;
  evaluationWindow: string;
  latencyThresholdMs?: number | null;
}

export interface UpdateSloRequest {
  name: string;
  description?: string;
  environmentName?: string | null;
  targetPercentage: number;
  evaluationWindow: string;
  latencyThresholdMs?: number | null;
  isEnabled: boolean;
}

// -------------------------------------------------------------
// Phase 6: Enterprise Administration, Audit, Reports & Settings
// -------------------------------------------------------------

export interface UserDto {
  id: string;
  userName: string;
  email: string;
  fullName: string;
  roles: string[];
  isActive: boolean;
  isLockedOut: boolean;
  lockoutEnd?: string | null;
  createdAtUtc: string;
}

export interface UserDetailDto extends UserDto {
  applicationMemberships: ApplicationMemberDto[];
  recentSecurityActivity: SecurityActivityDto[];
}

export interface ApplicationMemberDto {
  id: string;
  applicationId: string;
  applicationName: string;
  userId: string;
  userName: string;
  role: string;
  createdAtUtc: string;
}

export interface SecurityActivityDto {
  id: string;
  action: string;
  outcome: string;
  details: string;
  ipAddress?: string | null;
  timestampUtc: string;
}

export interface GetUsersRequest {
  page?: number;
  pageSize?: number;
  search?: string;
  role?: string;
  status?: string;
  fromUtc?: string;
  toUtc?: string;
}

export interface AuditEventDto {
  id: string;
  timestampUtc: string;
  actorId?: string | null;
  actorName?: string | null;
  action: string;
  targetEntityType: string;
  targetEntityId: string;
  applicationScope?: string | null;
  outcome: string;
  correlationId?: string | null;
  ipAddress?: string | null;
  userAgent?: string | null;
  details: string;
  metadataJson?: string | null;
}

export interface TopFacetCountDto {
  key: string;
  count: number;
}

export interface AuditSummaryDto {
  totalEvents24h: number;
  failureCount24h: number;
  topActions: TopFacetCountDto[];
  topActors: TopFacetCountDto[];
}

export interface GetAuditEventsRequest {
  page?: number;
  pageSize?: number;
  search?: string;
  actor?: string;
  action?: string;
  targetType?: string;
  outcome?: string;
  applicationScope?: string;
  fromUtc?: string;
  toUtc?: string;
}

export interface ReportTypeOptionDto {
  id: string;
  name: string;
  description: string;
  category: string;
}

export interface ReportPreviewDto {
  reportType: string;
  title: string;
  description: string;
  startUtc: string;
  endUtc: string;
  generatedAtUtc: string;
  applicationId?: string | null;
  applicationName?: string | null;
  summaryMetrics: Record<string, any>;
  columnHeaders: string[];
  rows: string[][];
}

export interface GenerateReportRequest {
  reportType: string;
  startUtc?: string;
  endUtc?: string;
  timeRange?: string;
  applicationId?: string;
  environmentName?: string;
  serviceName?: string;
}

export interface RetentionPolicyDto {
  logsRetentionDays: number;
  tracesRetentionDays: number;
  metricsRetentionDays: number;
  alertEvaluationsRetentionDays: number;
  resolvedIncidentsRetentionDays: number;
  notificationsRetentionDays: number;
  auditLogsRetentionDays: number;
  lastExecutedUtc: string;
  lastExecutedBy: string;
}

export interface RetentionTableEstimateDto {
  tableName: string;
  retentionDays: number;
  cutoffUtc: string;
  estimatedCount: number;
  oldestRecordUtc?: string | null;
}

export interface RetentionDryRunResultDto {
  estimatedAtUtc: string;
  logs: RetentionTableEstimateDto;
  traces: RetentionTableEstimateDto;
  spans: RetentionTableEstimateDto;
  metrics: RetentionTableEstimateDto;
  alertEvaluations: RetentionTableEstimateDto;
  resolvedIncidents: RetentionTableEstimateDto;
  notifications: RetentionTableEstimateDto;
  auditLogs: RetentionTableEstimateDto;
  totalEstimatedRecordsToDelete: number;
}

export interface RetentionExecutionResultDto {
  success: boolean;
  message: string;
  executedAtUtc: string;
  deletedCountsByTable: Record<string, number>;
  totalDeletedRecords: number;
}

export interface SystemSettingsDto {
  platformName: string;
  environmentName: string;
  publicRegistrationEnabled: boolean;
  alertEvaluationIntervalSeconds: number;
  maxNotificationRetries: number;
  webhookTimeoutSeconds: number;
  systemNoticeBanner: string;
  systemNoticeBannerActive: boolean;
  updatedAtUtc: string;
  updatedBy: string;
}

export interface ComponentHealthDto {
  name: string;
  status: string;
  message: string;
  latencyMs: number;
  details?: Record<string, string>;
}

export interface SystemHealthDto {
  status: string;
  checkedAtUtc: string;
  version: string;
  database: ComponentHealthDto;
  redis: ComponentHealthDto;
  ingestionEngine: ComponentHealthDto;
  alertEvaluator: ComponentHealthDto;
  notificationDispatcher: ComponentHealthDto;
}

// User & Access API methods
export async function getUsers(params?: GetUsersRequest): Promise<PagedResponse<UserDto>> {
  const res = await apiClient.get<ApiResponse<PagedResponse<UserDto>>>("/users", { params });
  return res.data.data;
}

export async function getUserDetail(id: string): Promise<UserDetailDto> {
  const res = await apiClient.get<ApiResponse<UserDetailDto>>(`/users/${id}`);
  return res.data.data;
}

export async function updateUserRole(id: string, roles: string[]): Promise<boolean> {
  const res = await apiClient.put<ApiResponse<boolean>>(`/users/${id}/roles`, { roles });
  return res.data.success;
}

export async function updateUserStatus(id: string, isActive: boolean, isLocked: boolean, lockoutDurationMinutes?: number): Promise<boolean> {
  const res = await apiClient.put<ApiResponse<boolean>>(`/users/${id}/status`, { isActive, isLocked, lockoutDurationMinutes });
  return res.data.success;
}

export async function revokeUserSessions(id: string): Promise<boolean> {
  const res = await apiClient.post<ApiResponse<boolean>>(`/users/${id}/revoke-sessions`);
  return res.data.success;
}

export async function adminResetPassword(id: string, newPassword: string, confirmPassword: string): Promise<boolean> {
  const res = await apiClient.post<ApiResponse<boolean>>(`/users/${id}/reset-password`, { newPassword, confirmPassword });
  return res.data.success;
}

export async function getApplicationMembers(appId: string): Promise<ApplicationMemberDto[]> {
  const res = await apiClient.get<ApiResponse<ApplicationMemberDto[]>>(`/users/applications/${appId}/members`);
  return res.data.data;
}

export async function assignApplicationMember(appId: string, userId: string, role: string): Promise<boolean> {
  const res = await apiClient.post<ApiResponse<boolean>>(`/users/applications/${appId}/members`, { userId, role });
  return res.data.success;
}

export async function removeApplicationMember(appId: string, userId: string): Promise<boolean> {
  const res = await apiClient.delete<ApiResponse<boolean>>(`/users/applications/${appId}/members/${userId}`);
  return res.data.success;
}

// Audit API methods
export async function getAuditEvents(params?: GetAuditEventsRequest): Promise<PagedResponse<AuditEventDto>> {
  const res = await apiClient.get<ApiResponse<PagedResponse<AuditEventDto>>>("/audit", { params });
  return res.data.data;
}

export async function getAuditSummary(): Promise<AuditSummaryDto> {
  const res = await apiClient.get<ApiResponse<AuditSummaryDto>>("/audit/summary");
  return res.data.data;
}

export async function getAuditEventDetail(id: string): Promise<AuditEventDto> {
  const res = await apiClient.get<ApiResponse<AuditEventDto>>(`/audit/${id}`);
  return res.data.data;
}

// Reports API methods
export async function getReportTypes(): Promise<ReportTypeOptionDto[]> {
  const res = await apiClient.get<ApiResponse<ReportTypeOptionDto[]>>("/reports/types");
  return res.data.data;
}

export async function previewReport(req: GenerateReportRequest): Promise<ReportPreviewDto> {
  const res = await apiClient.post<ApiResponse<ReportPreviewDto>>("/reports/preview", req);
  return res.data.data;
}

export async function exportReportCsv(req: GenerateReportRequest): Promise<Blob> {
  const res = await apiClient.post("/reports/export/csv", { ...req, format: "csv" }, { responseType: "blob" });
  return res.data;
}

export async function exportReportPrint(req: GenerateReportRequest): Promise<Blob> {
  const res = await apiClient.post("/reports/export/print", { ...req, format: "html" }, { responseType: "blob" });
  return res.data;
}

export async function exportReportPdf(req: GenerateReportRequest): Promise<Blob> {
  const res = await apiClient.post("/reports/export/pdf", { ...req, format: "pdf" }, { responseType: "blob" });
  return res.data;
}

// Retention & System API methods
export async function getRetentionPolicy(): Promise<RetentionPolicyDto> {
  const res = await apiClient.get<ApiResponse<RetentionPolicyDto>>("/retention/policy");
  return res.data.data;
}

export async function updateRetentionPolicy(req: Partial<RetentionPolicyDto>): Promise<RetentionPolicyDto> {
  const res = await apiClient.put<ApiResponse<RetentionPolicyDto>>("/retention/policy", req);
  return res.data.data;
}

export async function previewRetentionCleanup(): Promise<RetentionDryRunResultDto> {
  const res = await apiClient.post<ApiResponse<RetentionDryRunResultDto>>("/retention/preview");
  return res.data.data;
}

export async function executeRetentionCleanup(confirm: boolean, batchSize = 5000): Promise<RetentionExecutionResultDto> {
  const res = await apiClient.post<ApiResponse<RetentionExecutionResultDto>>("/retention/execute", { confirm, batchSize });
  return res.data.data;
}

export async function getSystemSettings(): Promise<SystemSettingsDto> {
  const res = await apiClient.get<ApiResponse<SystemSettingsDto>>("/system/settings");
  return res.data.data;
}

export async function updateSystemSettings(req: Partial<SystemSettingsDto>): Promise<SystemSettingsDto> {
  const res = await apiClient.put<ApiResponse<SystemSettingsDto>>("/system/settings", req);
  return res.data.data;
}

export async function getSystemHealth(): Promise<SystemHealthDto> {
  const res = await apiClient.get<ApiResponse<SystemHealthDto>>("/system/health");
  return res.data.data;
}

// ==========================================
// Organization & Multi-Tenant SaaS APIs
// ==========================================

export interface OrganizationDto {
  id: string;
  name: string;
  slug: string;
  description: string | null;
  isActive: boolean;
  userRole: string;
  membersCount: number;
  applicationsCount: number;
  createdAtUtc: string;
}

export interface OrganizationDetailDto {
  id: string;
  name: string;
  slug: string;
  description: string | null;
  isActive: boolean;
  userRole: string;
  createdAtUtc: string;
  members: OrganizationMemberDto[];
  pendingInvitations: OrganizationInvitationDto[];
  settings: OrganizationSettingsDto;
  usage: OrganizationUsageDto;
}

export interface OrganizationMemberDto {
  id: string;
  organizationId: string;
  userId: string;
  userName: string;
  email: string;
  fullName: string;
  role: string;
  joinedAtUtc: string;
}

export interface OrganizationInvitationDto {
  id: string;
  organizationId: string;
  email: string;
  role: string;
  expiresAtUtc: string;
  invitedByUserName: string;
  createdAtUtc: string;
  isPending: boolean;
}

export interface OrganizationSettingsDto {
  allowMemberInvites: boolean;
  requireTwoFactor: boolean;
  defaultRetentionDays: number;
  planTier?: string;
  maxApplications?: number;
  maxMembers?: number;
  maxCustomDashboards?: number;
  maxMonthlyLogs?: number;
  maxMonthlyTraces?: number;
}

export interface OrganizationUsageDto {
  logsIngestedMonthToDate: number;
  tracesIngestedMonthToDate: number;
  metricsIngestedMonthToDate: number;
  apiRequestsMonthToDate: number;
  estimatedStorageBytes: number;
  lastResetAtUtc: string;
}

export interface InvitationCreatedResponseDto {
  invitationId: string;
  email: string;
  role: string;
  expiresAtUtc: string;
  rawToken: string;
}

export async function getUserOrganizations(): Promise<OrganizationDto[]> {
  const res = await apiClient.get<ApiResponse<OrganizationDto[]>>("/organizations");
  return res.data.data;
}

export async function getOrganizationById(id: string): Promise<OrganizationDetailDto> {
  const res = await apiClient.get<ApiResponse<OrganizationDetailDto>>(`/organizations/${id}`);
  return res.data.data;
}

export async function createOrganization(req: { name: string; slug: string; description?: string }): Promise<OrganizationDto> {
  const res = await apiClient.post<ApiResponse<OrganizationDto>>("/organizations", req);
  return res.data.data;
}

export async function updateOrganization(id: string, req: { name: string; description?: string }): Promise<void> {
  await apiClient.put<ApiResponse<any>>(`/organizations/${id}`, req);
}

export async function getOrganizationMembers(id: string): Promise<OrganizationMemberDto[]> {
  const res = await apiClient.get<ApiResponse<OrganizationMemberDto[]>>(`/organizations/${id}/members`);
  return res.data.data;
}

export async function inviteOrganizationMember(id: string, req: { email: string; role: string }): Promise<InvitationCreatedResponseDto> {
  const res = await apiClient.post<ApiResponse<InvitationCreatedResponseDto>>(`/organizations/${id}/members/invite`, req);
  return res.data.data;
}

export async function acceptOrganizationInvitation(req: { token: string }): Promise<void> {
  await apiClient.post<ApiResponse<any>>("/organizations/invitations/accept", req);
}

export async function revokeOrganizationInvitation(id: string, invitationId: string): Promise<void> {
  await apiClient.delete<ApiResponse<any>>(`/organizations/${id}/invitations/${invitationId}`);
}

export async function removeOrganizationMember(id: string, userId: string): Promise<void> {
  await apiClient.delete<ApiResponse<any>>(`/organizations/${id}/members/${userId}`);
}

export async function changeOrganizationMemberRole(id: string, userId: string, role: string): Promise<void> {
  await apiClient.put<ApiResponse<any>>(`/organizations/${id}/members/${userId}/role`, { role });
}

export async function updateOrganizationSettings(id: string, req: OrganizationSettingsDto): Promise<void> {
  await apiClient.put<ApiResponse<any>>(`/organizations/${id}/settings`, req);
}

// ==========================================
// Custom Dashboards & Widget Builder APIs
// ==========================================

export interface DashboardSummaryItemDto {
  id: string;
  organizationId: string;
  title: string;
  description: string | null;
  isDefault: boolean;
  isShared: boolean;
  createdByUserName: string | null;
  refreshIntervalSeconds: number;
  defaultTimeRange: string;
  tags: string[];
  widgetCount: number;
  createdAtUtc: string;
  updatedAtUtc: string | null;
}

export interface DashboardWidgetDto {
  id: string;
  type: string;
  title: string;
  x: number;
  y: number;
  w: number;
  h: number;
  configJson: string;
}

export interface DashboardDetailDto extends DashboardSummaryItemDto {
  layoutJson: string;
  widgets: DashboardWidgetDto[];
}

export interface CreateDashboardRequest {
  title: string;
  description?: string;
  isDefault?: boolean;
  isShared?: boolean;
  refreshIntervalSeconds?: number;
  defaultTimeRange?: string;
  tags?: string[];
  layoutJson?: string;
}

export interface UpdateDashboardRequest {
  title: string;
  description?: string;
  isDefault?: boolean;
  isShared?: boolean;
  refreshIntervalSeconds?: number;
  defaultTimeRange?: string;
  tags?: string[];
  layoutJson?: string;
}

export interface ImportDashboardRequest {
  title: string;
  description?: string;
  layoutJson: string;
  tags?: string[];
}

export async function getDashboards(): Promise<DashboardSummaryItemDto[]> {
  const res = await apiClient.get<ApiResponse<DashboardSummaryItemDto[]>>("/dashboards");
  return res.data.data;
}

export async function getDashboardById(id: string): Promise<DashboardDetailDto> {
  const res = await apiClient.get<ApiResponse<DashboardDetailDto>>(`/dashboards/${id}`);
  return res.data.data;
}

export async function createDashboard(req: CreateDashboardRequest): Promise<DashboardDetailDto> {
  const res = await apiClient.post<ApiResponse<DashboardDetailDto>>("/dashboards", req);
  return res.data.data;
}

export async function updateDashboard(id: string, req: UpdateDashboardRequest): Promise<DashboardDetailDto> {
  const res = await apiClient.put<ApiResponse<DashboardDetailDto>>(`/dashboards/${id}`, req);
  return res.data.data;
}

export async function deleteDashboard(id: string): Promise<boolean> {
  const res = await apiClient.delete<ApiResponse<boolean>>(`/dashboards/${id}`);
  return res.data.success;
}

export async function duplicateDashboard(id: string): Promise<DashboardDetailDto> {
  const res = await apiClient.post<ApiResponse<DashboardDetailDto>>(`/dashboards/${id}/duplicate`);
  return res.data.data;
}

export async function importDashboard(req: ImportDashboardRequest): Promise<DashboardDetailDto> {
  const res = await apiClient.post<ApiResponse<DashboardDetailDto>>("/dashboards/import", req);
  return res.data.data;
}

export async function exportDashboard(id: string): Promise<DashboardDetailDto> {
  const res = await apiClient.get<ApiResponse<DashboardDetailDto>>(`/dashboards/${id}/export`);
  return res.data.data;
}

export async function instantiateDashboardTemplate(templateKey: string): Promise<DashboardDetailDto> {
  const res = await apiClient.post<ApiResponse<DashboardDetailDto>>(`/dashboards/templates/${templateKey}/instantiate`);
  return res.data.data;
}

// ==========================================
// Advanced APM & Service Map APIs
// ==========================================

export interface ServiceMapDto {
  nodes: ServiceMapNodeDto[];
  edges: ServiceMapEdgeDto[];
  computedAtUtc: string;
  timeRange: string;
}

export interface ServiceMapNodeDto {
  id: string;
  serviceName: string;
  displayName: string;
  environmentName: string;
  throughput: number;
  errorRate: number;
  avgLatencyMs: number;
  p95LatencyMs: number;
  healthStatus: string;
  totalCalls: number;
  errorCount: number;
}

export interface ServiceMapEdgeDto {
  sourceService: string;
  targetService: string;
  callCount: number;
  errorCount: number;
  errorRate: number;
  avgLatencyMs: number;
  p95LatencyMs: number;
}

export interface ServiceDependenciesDto {
  serviceName: string;
  inbound: ServiceMapEdgeDto[];
  outbound: ServiceMapEdgeDto[];
}

export interface EndpointPerformanceDto {
  endpointKey: string;
  method: string;
  route: string;
  serviceName: string;
  environmentName: string;
  throughputPerMin: number;
  totalCalls: number;
  errorCount: number;
  errorRate: number;
  avgDurationMs: number;
  p50DurationMs: number;
  p95DurationMs: number;
  p99DurationMs: number;
}

export interface LatencyBucketDto {
  rangeLabel: string;
  count: number;
}

export interface ErrorCategoryDto {
  statusCodeOrType: string;
  count: number;
}

export interface EndpointBreakdownDto {
  endpointKey: string;
  method: string;
  route: string;
  serviceName: string;
  throughputPerMin: number;
  avgDurationMs: number;
  p95DurationMs: number;
  errorRate: number;
  latencyBuckets: LatencyBucketDto[];
  errorBreakdown: ErrorCategoryDto[];
  sampleTraceIds: string[];
}

export interface ExternalDependencyDto {
  type: string;
  targetHostOrSystem: string;
  operationOrDb: string;
  callCount: number;
  errorCount: number;
  errorRate: number;
  avgDurationMs: number;
  p95DurationMs: number;
}

export async function getServiceMap(timeRange = "1h", environment?: string): Promise<ServiceMapDto> {
  const res = await apiClient.get<ApiResponse<ServiceMapDto>>("/apm/service-map", {
    params: { timeRange, environment }
  });
  return res.data.data;
}

export async function getServiceDependencies(serviceName: string, timeRange = "1h"): Promise<ServiceDependenciesDto> {
  const res = await apiClient.get<ApiResponse<ServiceDependenciesDto>>(`/apm/services/${serviceName}/dependencies`, {
    params: { timeRange }
  });
  return res.data.data;
}

export async function getEndpoints(timeRange = "1h", service?: string, search?: string): Promise<EndpointPerformanceDto[]> {
  const res = await apiClient.get<ApiResponse<EndpointPerformanceDto[]>>("/apm/endpoints", {
    params: { timeRange, service, search }
  });
  return res.data.data;
}

export async function getEndpointBreakdown(endpointKey: string, timeRange = "1h"): Promise<EndpointBreakdownDto> {
  const res = await apiClient.get<ApiResponse<EndpointBreakdownDto>>(`/apm/endpoints/${encodeURIComponent(endpointKey)}/breakdown`, {
    params: { timeRange }
  });
  return res.data.data;
}

export async function getExternalDependencies(timeRange = "1h"): Promise<ExternalDependencyDto[]> {
  const res = await apiClient.get<ApiResponse<ExternalDependencyDto[]>>("/apm/external-dependencies", {
    params: { timeRange }
  });
  return res.data.data;
}

// ==========================================
// AI Incident Intelligence APIs
// ==========================================

export interface RootCauseHypothesisDto {
  id: string;
  title: string;
  description: string;
  confidenceScore: number;
  evidence: string[];
  recommendedDiagnostics: string[];
}

export interface CorrelatedTelemetryDto {
  logCount: number;
  topExceptions: string[];
  anomalousMetrics: string[];
  slowTraceIds: string[];
}

export interface SimilarIncidentDto {
  incidentId: string;
  title: string;
  severity: string;
  resolvedAtUtc: string | null;
  resolutionNotes: string;
  mttrMinutes: number;
}

export interface IncidentAnalysisResult {
  incidentId: string;
  summary: string;
  providerName: string;
  generatedAtUtc: string;
  hypotheses: RootCauseHypothesisDto[];
  correlatedTelemetry: CorrelatedTelemetryDto;
  similarIncidents: SimilarIncidentDto[];
}

export interface IncidentPostmortemResult {
  incidentId: string;
  title: string;
  markdownContent: string;
  generatedAtUtc: string;
}

export interface RemediationActionDto {
  stepNumber: number;
  title: string;
  commandOrAction: string;
  riskLevel: string;
  rationale: string;
}

export interface RemediationSuggestionsResult {
  incidentId: string;
  actions: RemediationActionDto[];
}

export interface IntelligenceFeedbackRequest {
  hypothesisId: string;
  isHelpful: boolean;
  comment?: string;
}

export async function analyzeIncident(incidentId: string): Promise<IncidentAnalysisResult> {
  const res = await apiClient.post<ApiResponse<IncidentAnalysisResult>>(`/incidents/${incidentId}/intelligence/analyze`);
  return res.data.data;
}

export async function getIncidentAnalysis(incidentId: string): Promise<IncidentAnalysisResult> {
  const res = await apiClient.get<ApiResponse<IncidentAnalysisResult>>(`/incidents/${incidentId}/intelligence/analysis`);
  return res.data.data;
}

export async function generateIncidentPostmortem(incidentId: string): Promise<IncidentPostmortemResult> {
  const res = await apiClient.post<ApiResponse<IncidentPostmortemResult>>(`/incidents/${incidentId}/intelligence/postmortem`);
  return res.data.data;
}

export async function suggestIncidentRemediation(incidentId: string): Promise<RemediationSuggestionsResult> {
  const res = await apiClient.post<ApiResponse<RemediationSuggestionsResult>>(`/incidents/${incidentId}/intelligence/remediate`);
  return res.data.data;
}

export async function submitIncidentFeedback(incidentId: string, req: IntelligenceFeedbackRequest): Promise<boolean> {
  const res = await apiClient.post<ApiResponse<boolean>>(`/incidents/${incidentId}/intelligence/feedback`, req);
  return res.data.data;
}

// Status Pages Types
export interface StatusPageComponentDto {
  id: string;
  name: string;
  description?: string | null;
  applicationId?: string | null;
  serviceName?: string | null;
  statusOverride?: string | null;
  computedStatus: string;
}

export interface StatusPageMaintenanceWindowDto {
  id: string;
  title: string;
  description?: string | null;
  startTimeUtc: string;
  endTimeUtc: string;
  status: string;
  affectedComponentIds: string[];
}

export interface StatusPageIncidentUpdateDto {
  id: string;
  status: string;
  message: string;
  createdAtUtc: string;
  postedBy: string;
}

export interface StatusPageIncidentDto {
  id: string;
  statusPageId: string;
  linkedIncidentId?: string | null;
  title: string;
  status: string;
  severity: string;
  affectedComponentIds: string[];
  updates: StatusPageIncidentUpdateDto[];
  createdAtUtc: string;
  resolvedAtUtc?: string | null;
}

export interface StatusPageDto {
  id: string;
  organizationId: string;
  name: string;
  slug: string;
  description?: string | null;
  logoUrl?: string | null;
  supportUrl?: string | null;
  headerNotice?: string | null;
  isPublic: boolean;
  autoRollupEnabled: boolean;
  components: StatusPageComponentDto[];
  maintenanceWindows: StatusPageMaintenanceWindowDto[];
  activeIncidentsCount: number;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
}

export interface PublicStatusPageDto {
  name: string;
  slug: string;
  description?: string | null;
  logoUrl?: string | null;
  supportUrl?: string | null;
  headerNotice?: string | null;
  overallStatus: string;
  components: StatusPageComponentDto[];
  activeIncidents: StatusPageIncidentDto[];
  pastIncidents: StatusPageIncidentDto[];
  scheduledMaintenance: StatusPageMaintenanceWindowDto[];
  generatedAtUtc: string;
}

export interface CreateStatusPageRequest {
  name: string;
  slug: string;
  description?: string;
  logoUrl?: string;
  supportUrl?: string;
  headerNotice?: string;
  isPublic?: boolean;
  autoRollupEnabled?: boolean;
  components?: StatusPageComponentDto[];
  maintenanceWindows?: StatusPageMaintenanceWindowDto[];
}

export interface UpdateStatusPageRequest {
  name: string;
  slug: string;
  description?: string;
  logoUrl?: string;
  supportUrl?: string;
  headerNotice?: string;
  isPublic?: boolean;
  autoRollupEnabled?: boolean;
  components?: StatusPageComponentDto[];
  maintenanceWindows?: StatusPageMaintenanceWindowDto[];
}

export interface CreateStatusPageIncidentRequest {
  title: string;
  status: string;
  severity: string;
  linkedIncidentId?: string;
  affectedComponentIds?: string[];
  initialMessage: string;
}

export interface AddStatusPageIncidentUpdateRequest {
  status: string;
  message: string;
}

// Status Pages API methods
export async function getStatusPages(): Promise<StatusPageDto[]> {
  const res = await apiClient.get<ApiResponse<StatusPageDto[]>>("/status-pages");
  return res.data.data;
}

export async function getStatusPageById(id: string): Promise<StatusPageDto> {
  const res = await apiClient.get<ApiResponse<StatusPageDto>>(`/status-pages/${id}`);
  return res.data.data;
}

export async function createStatusPage(req: CreateStatusPageRequest): Promise<StatusPageDto> {
  const res = await apiClient.post<ApiResponse<StatusPageDto>>("/status-pages", req);
  return res.data.data;
}

export async function updateStatusPage(id: string, req: UpdateStatusPageRequest): Promise<StatusPageDto> {
  const res = await apiClient.put<ApiResponse<StatusPageDto>>(`/status-pages/${id}`, req);
  return res.data.data;
}

export async function deleteStatusPage(id: string): Promise<boolean> {
  const res = await apiClient.delete<ApiResponse<boolean>>(`/status-pages/${id}`);
  return res.data.data;
}

export async function postStatusPageIncident(
  statusPageId: string,
  req: CreateStatusPageIncidentRequest
): Promise<StatusPageIncidentDto> {
  const res = await apiClient.post<ApiResponse<StatusPageIncidentDto>>(`/status-pages/${statusPageId}/incidents`, req);
  return res.data.data;
}

export async function addStatusPageIncidentUpdate(
  statusPageId: string,
  incidentId: string,
  req: AddStatusPageIncidentUpdateRequest
): Promise<StatusPageIncidentDto> {
  const res = await apiClient.post<ApiResponse<StatusPageIncidentDto>>(
    `/status-pages/${statusPageId}/incidents/${incidentId}/updates`,
    req
  );
  return res.data.data;
}

export async function getPublicStatusPage(slug: string): Promise<PublicStatusPageDto> {
  const res = await apiClient.get<ApiResponse<PublicStatusPageDto>>(`/status/${slug}`);
  return res.data.data;
}

// Quota & Usage Types
export interface OrganizationQuotaStatusDto {
  organizationId: string;
  organizationName: string;
  planTier: string;
  logsUsed: number;
  logsQuota: number;
  logsPercentUsed: number;
  isLogsWarning: boolean;
  isLogsExceeded: boolean;
  tracesUsed: number;
  tracesQuota: number;
  tracesPercentUsed: number;
  isTracesWarning: boolean;
  isTracesExceeded: boolean;
  applicationsUsed: number;
  applicationsQuota: number;
  applicationsPercentUsed: number;
  dashboardsUsed: number;
  dashboardsQuota: number;
  dashboardsPercentUsed: number;
  membersUsed: number;
  membersQuota: number;
  membersPercentUsed: number;
  estimatedStorageBytes: number;
  hasAnyQuotaWarning: boolean;
  hasAnyQuotaExceeded: boolean;
}

export interface UpdateOrganizationQuotaRequest {
  planTier: string;
  maxApplications: number;
  maxMembers: number;
  maxCustomDashboards: number;
  maxMonthlyLogs: number;
  maxMonthlyTraces: number;
}

export async function getOrganizationQuota(orgId: string): Promise<OrganizationQuotaStatusDto> {
  const res = await apiClient.get<ApiResponse<OrganizationQuotaStatusDto>>(`/organizations/${orgId}/quota`);
  return res.data.data;
}

export async function updateOrganizationQuota(orgId: string, req: UpdateOrganizationQuotaRequest): Promise<boolean> {
  const res = await apiClient.put<ApiResponse<boolean>>(`/organizations/${orgId}/quota`, req);
  return res.data.success;
}

export async function getAllQuotasSummary(): Promise<OrganizationQuotaStatusDto[]> {
  const res = await apiClient.get<ApiResponse<OrganizationQuotaStatusDto[]>>("/organizations/quotas/summary");
  return res.data.data;
}

// ==========================================
// AI Observability & Predictive Intelligence APIs
// ==========================================

export interface TelemetryAnomalyDto {
  id: string;
  organizationId: string;
  applicationId: string;
  serviceName: string;
  signalType: string;
  detectionTimestamp: string;
  baselineValue: number;
  observedValue: number;
  deviationScore: number;
  confidenceScore: number;
  severity: string;
  detectionMethod: string;
  explanation: string;
  status: string;
  resolvedAtUtc?: string | null;
}

export interface AnomalyEvaluationRequest {
  organizationId: string;
  applicationId: string;
  serviceName: string;
  signalType: string;
  currentValue: number;
  historicalValues: number[];
}

export interface AnomalyDetectionResult {
  isAnomaly: boolean;
  severity: string;
  deviationScore: number;
  baselineValue: number;
  observedValue: number;
  confidenceScore: number;
  detectionMethod: string;
  explanation: string;
}

export interface LogPatternDto {
  id: string;
  organizationId: string;
  applicationId: string;
  serviceName: string;
  patternFingerprint: string;
  template: string;
  severity: string;
  totalOccurrences: number;
  firstSeenAtUtc: string;
  lastSeenAtUtc: string;
  sampleMessage: string;
  sampleTraceId?: string | null;
}

export interface LogPatternAnalysisResult {
  totalLogsAnalyzed: number;
  patternsFound: number;
  patterns: LogPatternDto[];
}

export interface RcaHypothesisDto {
  hypothesis: string;
  confidenceLevel: string; // "Likely cause" | "Possible cause" | "Insufficient evidence"
  confidenceScore: number;
  supportingEvidence: string[];
  contradictoryEvidence: string[];
  recommendedSteps: string[];
  isVerifiedByOperator: boolean;
  operatorNotes?: string | null;
}

export interface RcaAnalysisResult {
  incidentId: string;
  summary: string;
  impactedServices: string[];
  hypotheses: RcaHypothesisDto[];
  correlatedAnomalies: string[];
  correlatedTraceIds: string[];
  providerName: string;
  isDeterministic: boolean;
  generatedAtUtc: string;
}

export interface CapacityForecastRequest {
  organizationId: string;
  applicationId: string;
  serviceName: string;
  metricName: string;
  horizonMinutes: number;
  alertThreshold: number;
}

export interface CapacityForecastResult {
  applicationId: string;
  serviceName: string;
  metricName: string;
  currentValue: number;
  predictedValue: number;
  trendSlope: number;
  horizonMinutes: number;
  projectedBreachTimeUtc?: string | null;
  confidenceIntervalLower: number;
  confidenceIntervalUpper: number;
  sampleCount: number;
  calculationMethod: string;
  willBreachThreshold: boolean;
}

export interface PredictiveForecastDto {
  id: string;
  organizationId: string;
  applicationId: string;
  serviceName: string;
  metricName: string;
  currentValue: number;
  predictedValue: number;
  trendSlope: number;
  forecastHorizonMinutes: number;
  projectedBreachTimeUtc?: string | null;
  confidenceIntervalLower: number;
  confidenceIntervalUpper: number;
  calculationMethod: string;
  sampleCount: number;
  calculatedAtUtc: string;
  expiresAtUtc: string;
}

export interface CopilotChatRequest {
  incidentId: string;
  question: string;
  organizationId?: string;
}

export interface CopilotChatResponse {
  response: string;
  hypothesis: string;
  confidenceScore: number;
  safetyChecksPassed: boolean;
  recommendedActions: string[];
  requiresHumanApproval: boolean;
}

export interface AiUsageDto {
  organizationId: string;
  tokensUsedMonthToDate: number;
  invocationsMonthToDate: number;
  monthlyTokenBudget: number;
  remainingTokens: number;
  isQuotaExceeded: boolean;
  estimatedCostUsd: number;
}

export async function getAnomalies(applicationId: string): Promise<TelemetryAnomalyDto[]> {
  const res = await apiClient.get<ApiResponse<TelemetryAnomalyDto[]>>("/anomalies", {
    params: { applicationId }
  });
  return res.data.data;
}

export async function evaluateAnomaly(req: AnomalyEvaluationRequest): Promise<AnomalyDetectionResult> {
  const res = await apiClient.post<ApiResponse<AnomalyDetectionResult>>("/anomalies/evaluate", req);
  return res.data.data;
}

export async function resolveAnomaly(id: string): Promise<boolean> {
  const res = await apiClient.post<ApiResponse<boolean>>(`/anomalies/${id}/resolve`);
  return res.data.success;
}

export async function getLogPatterns(applicationId: string, serviceName?: string): Promise<LogPatternDto[]> {
  const res = await apiClient.get<ApiResponse<LogPatternDto[]>>("/logs/patterns", {
    params: { applicationId, serviceName }
  });
  return res.data.data;
}

export async function analyzeLogPatterns(applicationId: string, hours = 1): Promise<LogPatternAnalysisResult> {
  const res = await apiClient.post<ApiResponse<LogPatternAnalysisResult>>("/logs/patterns/analyze", null, {
    params: { applicationId, hours }
  });
  return res.data.data;
}

export async function performRca(req: { incidentId: string; correlationWindowMinutes?: number }): Promise<RcaAnalysisResult> {
  const res = await apiClient.post<ApiResponse<RcaAnalysisResult>>("/incidentintelligence/rca", {
    incidentId: req.incidentId,
    correlationWindow: req.correlationWindowMinutes ? `${req.correlationWindowMinutes * 60}s` : undefined
  });
  return res.data.data;
}

export async function verifyRcaHypothesis(rcaId: string, hypothesisIndex: number, verified: boolean, notes?: string): Promise<boolean> {
  const res = await apiClient.post<ApiResponse<boolean>>(`/incidentintelligence/rca/${rcaId}/verify`, {
    hypothesisIndex,
    verified,
    notes
  });
  return res.data.success;
}

export async function getActiveForecasts(applicationId: string): Promise<PredictiveForecastDto[]> {
  const res = await apiClient.get<ApiResponse<PredictiveForecastDto[]>>("/forecasts", {
    params: { applicationId }
  });
  return res.data.data;
}

export async function evaluateForecast(req: CapacityForecastRequest): Promise<CapacityForecastResult> {
  const res = await apiClient.post<ApiResponse<CapacityForecastResult>>("/forecasts/evaluate", req);
  return res.data.data;
}

export async function askCopilot(req: CopilotChatRequest): Promise<CopilotChatResponse> {
  const res = await apiClient.post<ApiResponse<CopilotChatResponse>>("/incidentintelligence/copilot/chat", req);
  return res.data.data;
}

export async function getAiUsage(): Promise<AiUsageDto> {
  const res = await apiClient.get<ApiResponse<AiUsageDto>>("/incidentintelligence/copilot/usage");
  return res.data.data;
}

// -------------------------------------------------------------
// Phase 12: Self-Service Onboarding & SaaS API Client
// -------------------------------------------------------------

export interface OnboardingStatusDto {
  isCompleted: boolean;
  currentStep: string;
  organizationId?: string;
  organizationName?: string;
  organizationSlug?: string;
  planTier?: string;
  applicationId?: string;
  applicationName?: string;
  hasApiKey: boolean;
  apiKeyPrefix?: string;
  hasReceivedTelemetry: boolean;
  firstTelemetryReceivedAtUtc?: string;
}

export interface OnboardingProvisionRequest {
  organizationName: string;
  organizationSlug?: string;
  planTier: string;
  applicationName: string;
  applicationDisplayName?: string;
  environmentName?: string;
}

export interface OnboardingProvisionedResponse {
  organizationId: string;
  organizationName: string;
  organizationSlug: string;
  applicationId: string;
  applicationName: string;
  apiKeyId: string;
  rawApiKey: string;
  keyPrefix: string;
  planTier: string;
  subscriptionStatus: string;
  trialEndsAtUtc?: string;
  sdkSnippetCSharp: string;
  sdkSnippetNodeJs: string;
  sdkSnippetPython: string;
  otelCollectorYaml: string;
}

export interface FirstTelemetryVerificationResponse {
  applicationId: string;
  hasReceivedTelemetry: boolean;
  logsCount: number;
  tracesCount: number;
  metricsCount: number;
  firstReceivedAtUtc?: string;
  lastReceivedAtUtc?: string;
}

export async function getOnboardingStatus(): Promise<OnboardingStatusDto> {
  const res = await apiClient.get<ApiResponse<OnboardingStatusDto>>("/onboarding/status");
  return res.data.data;
}

export async function provisionOnboardingWorkspace(req: OnboardingProvisionRequest): Promise<OnboardingProvisionedResponse> {
  const res = await apiClient.post<ApiResponse<OnboardingProvisionedResponse>>("/onboarding/provision", req);
  return res.data.data;
}

export async function verifyFirstTelemetry(applicationId: string): Promise<FirstTelemetryVerificationResponse> {
  const res = await apiClient.get<ApiResponse<FirstTelemetryVerificationResponse>>(`/onboarding/verify/${applicationId}`);
  return res.data.data;
}

export async function completeOnboarding(): Promise<boolean> {
  const res = await apiClient.post<ApiResponse<boolean>>("/onboarding/complete");
  return res.data.data;
}

// ==========================================
// Phase 12: SaaS Billing & Subscription APIs
// ==========================================

export interface SubscriptionPlanDto {
  id: string;
  name: string;
  description: string;
  monthlyPrice: number;
  annualPrice: number;
  currency: string;
  maxApplications: number;
  maxMembers: number;
  maxCustomDashboards: number;
  maxMonthlyLogs: number;
  maxMonthlyTraces: number;
  maxMonthlyAiTokens: number;
  dataRetentionDays: number;
  allowCustomReports: boolean;
  allowPrioritySupport: boolean;
}

export interface OrganizationSubscriptionDto {
  id: string;
  organizationId: string;
  planId: string;
  planName: string;
  status: string;
  billingInterval: string;
  currentPeriodStartUtc: string;
  currentPeriodEndUtc: string;
  trialEndsAtUtc?: string;
  gracePeriodEndsAtUtc?: string;
  canceledAtUtc?: string;
  cancelAtPeriodEnd: boolean;
  concurrencyToken: string;
}

export interface SaaSInvoiceDto {
  id: string;
  organizationId: string;
  invoiceNumber: string;
  status: string;
  subtotal: number;
  taxAmount: number;
  taxRatePercent: number;
  total: number;
  amountPaid: number;
  amountDue: number;
  currency: string;
  periodStartUtc: string;
  periodEndUtc: string;
  dueDateUtc: string;
  paidAtUtc?: string;
  hostedInvoiceUrl?: string;
  invoicePdfUrl?: string;
  createdAtUtc: string;
  lineItems: SaaSInvoiceLineItemDto[];
}

export interface SaaSInvoiceLineItemDto {
  id: string;
  description: string;
  quantity: number;
  unitPrice: number;
  amount: number;
}

export interface TenantEntitlementsDto {
  organizationId: string;
  planTier: string;
  subscriptionStatus: string;
  isSubscriptionActive: boolean;
  canRegisterApplication: boolean;
  canInviteMember: boolean;
  canCreateDashboard: boolean;
  canIngestLogs: boolean;
  canIngestTraces: boolean;
  canIngestMetrics: boolean;
  canExecuteAiAnalysis: boolean;
  allowCustomReports: boolean;
  allowApiAccess: boolean;
  allowExportData: boolean;
  dataRetentionDays: number;
}

export interface TenantUsageSummaryDto {
  organizationId: string;
  planTier: string;
  currentPeriodStartUtc: string;
  currentPeriodEndUtc: string;
  applicationsUsed: number;
  applicationsQuota: number;
  applicationsPercentUsed: number;
  membersUsed: number;
  membersQuota: number;
  membersPercentUsed: number;
  dashboardsUsed: number;
  dashboardsQuota: number;
  dashboardsPercentUsed: number;
  logsUsed: number;
  logsQuota: number;
  logsPercentUsed: number;
  isLogsWarning: boolean;
  isLogsExceeded: boolean;
  tracesUsed: number;
  tracesQuota: number;
  tracesPercentUsed: number;
  isTracesWarning: boolean;
  isTracesExceeded: boolean;
  aiTokensUsed: number;
  aiTokensQuota: number;
  aiTokensPercentUsed: number;
  isAiTokensWarning: boolean;
  isAiTokensExceeded: boolean;
  estimatedStorageBytes: number;
  hasAnyWarning: boolean;
  hasAnyExceeded: boolean;
}

export interface CheckoutSessionResponse {
  sessionId: string;
  checkoutUrl: string;
  provider: string;
}

export interface CustomerPortalResponse {
  portalUrl: string;
  provider: string;
}

export interface TenantDataExportDto {
  exportedAtUtc: string;
  organizationId: string;
  name: string;
  slug: string;
  description?: string;
  isActive: boolean;
  createdAtUtc: string;
  settings?: any;
  subscription?: any;
  members: any[];
  applications: any[];
  alertRules: any[];
  dashboards: any[];
  recentAuditEvents: any[];
}

export async function getSubscription(): Promise<OrganizationSubscriptionDto> {
  const res = await apiClient.get<ApiResponse<OrganizationSubscriptionDto>>("/billing/subscription");
  return res.data.data;
}

export async function getPlans(): Promise<SubscriptionPlanDto[]> {
  const res = await apiClient.get<ApiResponse<SubscriptionPlanDto[]>>("/billing/plans");
  return res.data.data;
}

export async function changePlan(req: { newPlanName: string; billingInterval?: string }): Promise<OrganizationSubscriptionDto> {
  const res = await apiClient.post<ApiResponse<OrganizationSubscriptionDto>>("/billing/subscription/change-plan", req);
  return res.data.data;
}

export async function cancelSubscription(req: { cancelImmediately?: boolean }): Promise<OrganizationSubscriptionDto> {
  const res = await apiClient.post<ApiResponse<OrganizationSubscriptionDto>>("/billing/subscription/cancel", req);
  return res.data.data;
}

export async function reactivateSubscription(): Promise<OrganizationSubscriptionDto> {
  const res = await apiClient.post<ApiResponse<OrganizationSubscriptionDto>>("/billing/subscription/reactivate");
  return res.data.data;
}

export async function getInvoices(): Promise<SaaSInvoiceDto[]> {
  const res = await apiClient.get<ApiResponse<SaaSInvoiceDto[]>>("/billing/invoices");
  return res.data.data;
}

export async function getInvoiceById(id: string): Promise<SaaSInvoiceDto> {
  const res = await apiClient.get<ApiResponse<SaaSInvoiceDto>>(`/billing/invoices/${id}`);
  return res.data.data;
}

export async function getEntitlements(): Promise<TenantEntitlementsDto> {
  const res = await apiClient.get<ApiResponse<TenantEntitlementsDto>>("/billing/entitlements");
  return res.data.data;
}

export async function getTenantUsage(): Promise<TenantUsageSummaryDto> {
  const res = await apiClient.get<ApiResponse<TenantUsageSummaryDto>>("/billing/usage");
  return res.data.data;
}

export async function createCheckoutSession(req: { planName: string; interval?: string }): Promise<CheckoutSessionResponse> {
  const res = await apiClient.post<ApiResponse<CheckoutSessionResponse>>("/billing/checkout-session", req);
  return res.data.data;
}

export async function createCustomerPortalSession(): Promise<CustomerPortalResponse> {
  const res = await apiClient.post<ApiResponse<CustomerPortalResponse>>("/billing/customer-portal");
  return res.data.data;
}

export async function transferOwnership(orgId: string, newOwnerUserId: string): Promise<boolean> {
  const res = await apiClient.post<ApiResponse<any>>(`/organizations/${orgId}/ownership-transfer`, {
    newOwnerUserId
  });
  return res.data.success;
}

export async function exportTenantData(orgId: string): Promise<TenantDataExportDto> {
  const res = await apiClient.post<ApiResponse<TenantDataExportDto>>(`/organizations/${orgId}/export`);
  return res.data.data;
}

export async function deleteOrganizationAccount(orgId: string, confirmationPhrase: string): Promise<boolean> {
  const res = await apiClient.delete<ApiResponse<any>>(`/organizations/${orgId}/account`, {
    data: { confirmationPhrase }
  });
  return res.data.success;
}









