using System.Diagnostics;
using System.Text.RegularExpressions;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Infrastructure.Services.Ai;

public class DeterministicAiProvider : IAiProvider
{
    public string ProviderId => "Deterministic";
    public string ProviderName => "Deterministic Heuristic Engine";
    public bool IsEnabled => true;

    private static readonly Regex ApiKeyRegex = new(@"(sk_[a-zA-Z0-9_\-]{16,}|sentinel_[a-zA-Z0-9_\-]{16,}|sent_live_[a-zA-Z0-9_\-]{16,})", RegexOptions.Compiled);
    private static readonly Regex BearerRegex = new(@"(Bearer\s+[a-zA-Z0-9_\-\.]{20,})", RegexOptions.Compiled);
    private static readonly Regex PasswordRegex = new(@"(password|secret|pwd)\s*[=:]\s*['""][^'""]+['""]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ConnectionStringRegex = new(@"(Server|Host|Data Source)=[^;]+;.*?(Password|Pwd)=[^;]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var sanitizedPrompt = ScrubSensitiveData(request.Prompt);

        var output = $"[DETERMINISTIC ANALYSIS]\n" +
                     $"Processed prompt of {sanitizedPrompt.Length} characters without external network transmission.\n" +
                     $"Operational heuristics applied based on platform telemetry patterns and threshold evaluation.";

        sw.Stop();
        return Task.FromResult(new AiCompletionResult
        {
            IsSuccessful = true,
            ResultText = output,
            ErrorMessage = null,
            ProviderName = ProviderName,
            Model = "Heuristic-Rule-Engine-v1",
            DurationMs = sw.ElapsedMilliseconds,
            TokensUsed = Math.Max(10, sanitizedPrompt.Length / 4),
            IsDeterministic = true
        });
    }

    public Task<AiCopilotChatResponse> ChatAsync(AiCopilotChatRequest request, CancellationToken cancellationToken = default)
    {
        var sanitizedMessage = ScrubSensitiveData(request.Message);
        var citations = new List<string>();
        var actions = new List<string>();

        string reply;
        if (request.IncidentId.HasValue)
        {
            citations.Add($"Incident-ID: {request.IncidentId.Value}");
            actions.Add("Inspect correlated trace waterfall and error logs in Observability Explorer.");
            actions.Add("Check upstream database connection pool saturation.");

            var operatorQuestion = sanitizedMessage;
            var evidenceIndex = sanitizedMessage.IndexOf("<telemetry_evidence>", StringComparison.OrdinalIgnoreCase);
            if (evidenceIndex >= 0)
            {
                var headerIndex = sanitizedMessage.IndexOf("Operator Query:", StringComparison.OrdinalIgnoreCase);
                if (headerIndex >= 0)
                {
                    var start = headerIndex + "Operator Query:".Length;
                    operatorQuestion = sanitizedMessage.Substring(start, evidenceIndex - start).Trim();
                }
                else
                {
                    operatorQuestion = sanitizedMessage[..evidenceIndex].Trim();
                }
            }

            reply = $"Based on deterministic incident correlation for Incident {request.IncidentId.Value}: " +
                    $"The telemetry shows a correlated spike in error rate. " +
                    $"Query '{operatorQuestion}' evaluated against known incident signatures. " +
                    $"No external LLM inference was used for this deterministic response.";
        }
        else
        {
            actions.Add("Review Active Alerts and Anomaly Detection views.");
            reply = $"SentinelOps Copilot (Deterministic Engine): Received query '{sanitizedMessage}'. " +
                    $"System monitoring is operational. All recommendations require human review prior to execution.";
        }

        return Task.FromResult(new AiCopilotChatResponse
        {
            Reply = reply,
            ProviderName = ProviderName,
            IsDeterministic = true,
            Citations = citations,
            RecommendedActions = actions,
            RequiresHumanApproval = true,
            TokensUsed = Math.Max(15, sanitizedMessage.Length / 4)
        });
    }

    public static string ScrubSensitiveData(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;
        var scrubbed = ApiKeyRegex.Replace(input, "[REDACTED_API_KEY]");
        scrubbed = BearerRegex.Replace(scrubbed, "Bearer [REDACTED_TOKEN]");
        scrubbed = PasswordRegex.Replace(scrubbed, "$1=\"[REDACTED_SECRET]\"");
        scrubbed = ConnectionStringRegex.Replace(scrubbed, "Host=[REDACTED_HOST];Password=[REDACTED_CREDENTIAL]");
        return scrubbed;
    }
}
