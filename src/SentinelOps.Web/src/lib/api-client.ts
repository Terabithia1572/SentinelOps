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
