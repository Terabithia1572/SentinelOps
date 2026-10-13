using FluentAssertions;
using NSubstitute;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Application.Common.Security;
using SentinelOps.Infrastructure.Services;
using Xunit;

namespace SentinelOps.Application.Tests.Services;

public class ReportingServiceTests
{
    private readonly IApplicationDbContext _context;
    private readonly ReportingService _service;

    public ReportingServiceTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _service = new ReportingService(_context);
    }

    [Fact]
    public void GetAvailableReportTypes_ShouldReturnAll6EnterpriseReportTypes()
    {
        var types = _service.GetAvailableReportTypes();

        types.Should().HaveCount(6);
        types.Select(t => t.Id).Should().Contain([
            "Performance",
            "ErrorsAndExceptions",
            "IncidentHistory",
            "AlertActivity",
            "SloCompliance",
            "ApplicationHealth"
        ]);
    }

    [Theory]
    [InlineData("=1+1", "'=1+1")]
    [InlineData("+cmd|' /C calc'!A0", "'+cmd|' /C calc'!A0")]
    [InlineData("-100", "'-100")]
    [InlineData("@SUM(A1:B10)", "'@SUM(A1:B10)")]
    [InlineData("\tmalicious_tab", "'\tmalicious_tab")]
    [InlineData("\rmalicious_cr", "'\rmalicious_cr")]
    public void Sanitize_WhenCellStartsWithFormulaTrigger_ShouldSanitizeWithLeadingQuote(
        string maliciousInput, string expectedSanitizedPrefix)
    {
        var sanitized = CsvSanitizer.Sanitize(maliciousInput);

        sanitized.Should().StartWith($"\"{expectedSanitizedPrefix}");
    }
}
