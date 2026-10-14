# SentinelOps — Checkpoint 11.6: Predictive Alerting & Capacity Forecasting

## 1. Executive Summary

Checkpoint 11.6 introduces mathematical, tenant-isolated, deterministic capacity forecasting and predictive alerting to SentinelOps. Unlike naive threshold monitors that only fire when a failure has already occurred, the `PredictiveAlertingService` computes statistical time-series linear regressions to model metric consumption trajectories, calculate 95% confidence intervals, and predict threshold breaches well in advance of system impairment.

All algorithms are fully explainable and deterministic—explicitly rejecting opaque "black box" heuristics while guaranteeing zero risk of hallucinated metrics.

---

## 2. Mathematical Foundation & Formulation

The capacity forecasting engine utilizes Ordinary Least Squares (OLS) linear regression across historical time-series metric points $(t_i, y_i)$:

### 2.1. Coordinate Normalization
Given metric measurements at timestamps $T_0, T_1, \dots, T_{n-1}$ with measured values $y_0, y_1, \dots, y_{n-1}$, time coordinates are normalized relative to the earliest sample $T_0$:
$$x_i = (T_i - T_0)_{\text{minutes}}$$

### 2.2. Slope and Intercept Calculation
$$\bar{x} = \frac{1}{n} \sum_{i=1}^n x_i, \quad \bar{y} = \frac{1}{n} \sum_{i=1}^n y_i$$

$$\hat{\beta}_1 = \frac{\sum_{i=1}^n (x_i - \bar{x})(y_i - \bar{y})}{\sum_{i=1}^n (x_i - \bar{x})^2}$$

$$\hat{\beta}_0 = \bar{y} - \hat{\beta}_1 \bar{x}$$

### 2.3. Horizon Projection
For a specified forecast horizon $H$ (in minutes), the predicted metric value at future time $x_{\text{future}} = x_{n-1} + H$ is:
$$\hat{y}(x_{\text{future}}) = \hat{\beta}_0 + \hat{\beta}_1 x_{\text{future}}$$

### 2.4. Residual Standard Error & 95% Confidence Intervals
Residual variance is quantified over $n - 2$ degrees of freedom:
$$SE = \sqrt{\frac{1}{n - 2} \sum_{i=1}^n (y_i - (\hat{\beta}_0 + \hat{\beta}_1 x_i))^2}$$

The 95% confidence bounds are evaluated as:
$$CI_{95\%} = \hat{y}(x_{\text{future}}) \pm 1.96 \cdot SE$$

### 2.5. Threshold Breach Time Projection
If the trend is increasing ($\hat{\beta}_1 > 0$) towards an upper critical threshold $Y_{\text{crit}} > y_{n-1}$:
$$x_{\text{breach}} = \frac{Y_{\text{crit}} - \hat{\beta}_0}{\hat{\beta}_1}$$
$$T_{\text{breach}} = T_0 + x_{\text{breach}} \text{ minutes}$$

A predictive alert is triggered if $x_{\text{breach}} - x_{n-1} \le H$, providing operators with lead time to scale compute, rotate logs, or expand storage volume.

---

## 3. Data Integrity & Safeguards

| Safeguard | Implementation Mechanism | Purpose |
| :--- | :--- | :--- |
| **Minimum Sample Gate** | Requires $n \ge 5$ discrete measurements | Prevents erratic projections on sparse or cold-start metrics |
| **Zero-Variance / Degenerate Slope Protection** | Guard $\sum (x_i - \bar{x})^2 > 10^{-9}$ | Prevents division-by-zero on identical timestamps |
| **Multi-Tenant Isolation** | Scoped strictly to `OrganizationId` & `ApplicationId` | Ensures cross-tenant metric isolation at DB query level |
| **Confidence Interval Visibility** | Reports lower & upper 95% bounds | Alerts operators to high data volatility ($SE \gg 0$) |
| **Automatic Forecast Expiration** | `ExpiresAtUtc = CalculatedAtUtc + max(15m, Horizon)` | Ensures stale forecasts do not linger in operator views |

---

## 4. API Endpoints

### 4.1. Evaluate Forecast: `POST /api/forecasts/evaluate`
```json
{
  "organizationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "applicationId": "7cb45d62-1234-5678-90ab-cdef12345678",
  "serviceName": "database-pool",
  "metricName": "connection_count",
  "horizonMinutes": 30,
  "alertThreshold": 120.0
}
```

**Response (`200 OK`):**
```json
{
  "success": true,
  "data": {
    "applicationId": "7cb45d62-1234-5678-90ab-cdef12345678",
    "serviceName": "database-pool",
    "metricName": "connection_count",
    "currentValue": 90.0,
    "predictedValue": 150.0,
    "trendSlope": 2.0,
    "horizonMinutes": 30,
    "projectedBreachTimeUtc": "2026-10-15T00:15:00Z",
    "confidenceIntervalLower": 145.2,
    "confidenceIntervalUpper": 154.8,
    "sampleCount": 8,
    "calculationMethod": "LinearRegressionOrdinaryLeastSquares",
    "willBreachThreshold": true
  },
  "message": null
}
```

### 4.2. Get Active Forecasts: `GET /api/forecasts?applicationId={appId}`
Retrieves all non-expired predictive forecasts for the authorized tenant and application.

---

## 5. Verification & Test Evidence

### Unit Tests (`PredictiveAlertingServiceTests.cs`):
- `ForecastMetricAsync_WhenInsufficientSamples_ReturnsInsufficientSamplesSafely`: PASS.
- `CalculateLinearForecast_WhenStableData_ForecastsConstantValueAndNoBreach`: PASS.
- `CalculateLinearForecast_WhenRapidlyIncreasingTrend_ForecastsBreachAccurately`: PASS.
- `CalculateLinearForecast_WhenDecreasingTrend_DoesNotBreachUpperThreshold`: PASS.
- `CalculateLinearForecast_CalculatesConfidenceIntervalsProperly`: PASS.
- `ForecastMetricAsync_WhenSufficientDatabaseMetrics_PersistsPredictiveForecast`: PASS.
- `GetActiveForecastsAsync_TenantIsolation_FiltersByOrganization`: PASS.

### Integration Tests (`PredictiveAlertingIntegrationTests.cs`):
- `Evaluate_Endpoint_ComputesAndReturnsForecast`: PASS (200 OK, full response schema verified).
- `GetActive_Forecasts_ReturnsPersistedForecasts`: PASS (200 OK, tenant filter verified).

---

## 6. Checkpoint Verdict

**VERDICT: PASS**
- Deterministic, explainable Ordinary Least Squares forecasting implemented.
- 95% confidence intervals derived from residual standard errors.
- Strict multi-tenant data boundaries enforced.
- Zero mock LLM or non-deterministic hallucinations.
