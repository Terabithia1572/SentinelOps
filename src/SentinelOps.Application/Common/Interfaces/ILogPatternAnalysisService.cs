using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Application.Common.Interfaces;

public interface ILogPatternAnalysisService
{
    LogPatternExtractionResult ExtractPattern(string rawMessage, string? exceptionDetail = null);
    Task<LogPatternAnalysisResult> AnalyzeApplicationLogsAsync(Guid applicationId, TimeSpan window, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LogPatternDto>> GetPatternsAsync(Guid applicationId, CancellationToken cancellationToken = default);
}
