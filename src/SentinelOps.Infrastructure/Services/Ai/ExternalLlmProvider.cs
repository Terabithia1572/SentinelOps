using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Infrastructure.Services.Ai;

public class ExternalLlmProvider : IAiProvider
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<AiOptions> _options;
    private readonly IAiUsageMeter _usageMeter;
    private readonly ILogger<ExternalLlmProvider> _logger;

    public string ProviderId => "ExternalLlm";
    public string ProviderName => "External Redacted LLM Provider";
    public bool IsEnabled => _options.Value.Enabled && _options.Value.ExternalProvider.IsConfigured;

    // Circuit breaker state
    private static int _consecutiveFailures = 0;
    private static DateTime _circuitBreakerTripUntil = DateTime.MinValue;
    private static readonly object _circuitLock = new();

    public ExternalLlmProvider(
        HttpClient httpClient,
        IOptions<AiOptions> options,
        IAiUsageMeter usageMeter,
        ILogger<ExternalLlmProvider> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _usageMeter = usageMeter;
        _logger = logger;
    }

    public async Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken cancellationToken = default)
    {
        var config = _options.Value.ExternalProvider;
        if (!config.IsConfigured)
        {
            return new AiCompletionResult
            {
                IsSuccessful = false,
                ErrorMessage = "External LLM is not configured. Set endpoint and API key to enable live inference.",
                ResultText = "External LLM integration is unconfigured. Fallback to deterministic heuristic engine recommended.",
                ProviderName = ProviderName,
                Model = config.Model,
                DurationMs = 0,
                TokensUsed = 0,
                IsDeterministic = false
            };
        }

        // Circuit breaker check
        lock (_circuitLock)
        {
            if (DateTime.UtcNow < _circuitBreakerTripUntil)
            {
                return new AiCompletionResult
                {
                    IsSuccessful = false,
                    ErrorMessage = "External LLM circuit breaker is OPEN due to repeated consecutive errors. Backing off.",
                    ResultText = "External provider temporarily unavailable. Please retry after circuit cooldown.",
                    ProviderName = ProviderName,
                    Model = config.Model,
                    DurationMs = 0,
                    TokensUsed = 0,
                    IsDeterministic = false
                };
            }
        }

        // Check organization quota
        var canProceed = await _usageMeter.CanExecuteAiOperationAsync(request.OrganizationId, request.MaxTokens, cancellationToken);
        if (!canProceed)
        {
            return new AiCompletionResult
            {
                IsSuccessful = false,
                ErrorMessage = "Organization monthly AI token quota exceeded or AI disabled in settings.",
                ResultText = "Quota exceeded. Upgrade organization quota or switch to deterministic provider.",
                ProviderName = ProviderName,
                Model = config.Model,
                DurationMs = 0,
                TokensUsed = 0,
                IsDeterministic = false
            };
        }

        var sw = Stopwatch.StartNew();
        var sanitizedPrompt = DeterministicAiProvider.ScrubSensitiveData(request.Prompt);
        var sanitizedSystemPrompt = string.IsNullOrEmpty(request.SystemPrompt)
            ? "You are an enterprise observability diagnostic assistant. Provide concise, factual analysis. Never generate destructive instructions."
            : DeterministicAiProvider.ScrubSensitiveData(request.SystemPrompt);

        // Retry loop with exponential backoff
        int attempts = 0;
        int maxRetries = Math.Max(1, config.MaxRetries);

        while (attempts < maxRetries)
        {
            attempts++;
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(Math.Max(5, config.TimeoutSeconds)));

                var payload = new
                {
                    model = config.Model,
                    messages = new object[]
                    {
                        new { role = "system", content = sanitizedSystemPrompt },
                        new { role = "user", content = sanitizedPrompt }
                    },
                    max_tokens = Math.Min(config.MaxTokens, request.MaxTokens),
                    temperature = Math.Clamp(request.Temperature, 0.0, 1.0)
                };

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, config.Endpoint);
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);
                httpRequest.Content = JsonContent.Create(payload);

                var response = await _httpClient.SendAsync(httpRequest, cts.Token);
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync(cts.Token);
                    using var doc = JsonDocument.Parse(responseJson);

                    var text = string.Empty;
                    int tokensUsed = Math.Max(1, sanitizedPrompt.Length / 4);

                    if (doc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                    {
                        var firstChoice = choices[0];
                        if (firstChoice.TryGetProperty("message", out var msg) && msg.TryGetProperty("content", out var content))
                        {
                            text = content.GetString() ?? string.Empty;
                        }
                    }

                    if (doc.RootElement.TryGetProperty("usage", out var usageElem) && usageElem.TryGetProperty("total_tokens", out var totalTokens))
                    {
                        tokensUsed = totalTokens.GetInt32();
                    }

                    // Reset circuit breaker
                    lock (_circuitLock)
                    {
                        _consecutiveFailures = 0;
                    }

                    await _usageMeter.RecordUsageAsync(request.OrganizationId, tokensUsed, cancellationToken: cancellationToken);
                    sw.Stop();

                    return new AiCompletionResult
                    {
                        IsSuccessful = true,
                        ResultText = text,
                        ProviderName = ProviderName,
                        Model = config.Model,
                        DurationMs = sw.ElapsedMilliseconds,
                        TokensUsed = tokensUsed,
                        IsDeterministic = false
                    };
                }

                _logger.LogWarning("External LLM call failed with HTTP status {Status} on attempt {Attempt}", response.StatusCode, attempts);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Exception calling External LLM on attempt {Attempt}", attempts);
            }

            if (attempts < maxRetries)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempts)), cancellationToken);
            }
        }

        sw.Stop();
        lock (_circuitLock)
        {
            _consecutiveFailures++;
            if (_consecutiveFailures >= 3)
            {
                _circuitBreakerTripUntil = DateTime.UtcNow.AddSeconds(60);
                _logger.LogError("External LLM circuit breaker tripped for 60s after {Failures} consecutive failures.", _consecutiveFailures);
            }
        }

        return new AiCompletionResult
        {
            IsSuccessful = false,
            ErrorMessage = $"External LLM call failed after {maxRetries} attempts.",
            ResultText = "External LLM could not be reached. Automated fallback to deterministic analysis recommended.",
            ProviderName = ProviderName,
            Model = config.Model,
            DurationMs = sw.ElapsedMilliseconds,
            TokensUsed = 0,
            IsDeterministic = false
        };
    }

    public async Task<AiCopilotChatResponse> ChatAsync(AiCopilotChatRequest request, CancellationToken cancellationToken = default)
    {
        var completionReq = new AiCompletionRequest
        {
            OrganizationId = request.OrganizationId,
            Prompt = request.Message,
            SystemPrompt = "You are SentinelOps Incident Copilot. Analyze the provided operational data. Distinguish facts from hypotheses. Recommend investigation steps. Never generate automated remediation commands without human operator review.",
            MaxTokens = 1000,
            Temperature = 0.2
        };

        var result = await CompleteAsync(completionReq, cancellationToken);

        if (!result.IsSuccessful)
        {
            // Graceful fallback to deterministic provider
            var fallback = new DeterministicAiProvider();
            var fallbackResponse = await fallback.ChatAsync(request, cancellationToken);
            fallbackResponse.Reply = $"[External LLM unavailable: {result.ErrorMessage}] Fallback to deterministic engine:\n\n{fallbackResponse.Reply}";
            return fallbackResponse;
        }

        return new AiCopilotChatResponse
        {
            Reply = result.ResultText,
            ProviderName = ProviderName,
            IsDeterministic = false,
            Citations = request.IncidentId.HasValue ? [$"Incident: {request.IncidentId.Value}"] : [],
            RecommendedActions = ["Review findings with on-call engineer before executing remediation."],
            RequiresHumanApproval = true,
            TokensUsed = result.TokensUsed
        };
    }
}
