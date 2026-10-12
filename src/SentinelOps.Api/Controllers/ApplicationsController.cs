using Microsoft.AspNetCore.Mvc;
using SentinelOps.Application.Applications.Commands.CreateApplication;
using SentinelOps.Application.Applications.Queries.GetApplications;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;
using SentinelOps.Contracts.Responses;

namespace SentinelOps.Api.Controllers;

public class ApplicationsController : BaseApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ApplicationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApplications(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetApplicationsQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ApplicationDto>>.Ok(result));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ApplicationDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateApplication(
        [FromBody] CreateApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateApplicationCommand
        {
            Name = request.Name,
            DisplayName = request.DisplayName,
            Description = request.Description
        };

        var result = await Mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetApplications), new { id = result.Id }, ApiResponse<ApplicationDto>.Ok(result, "Application created successfully"));
    }
}
