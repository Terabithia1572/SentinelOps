using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SentinelOps.Application.Applications.Commands.CreateApplication;
using SentinelOps.Application.Applications.Commands.CreateEnvironment;
using SentinelOps.Application.Applications.Commands.CreateService;
using SentinelOps.Application.Applications.Commands.DeleteApplication;
using SentinelOps.Application.Applications.Commands.DeleteEnvironment;
using SentinelOps.Application.Applications.Commands.UpdateApplication;
using SentinelOps.Application.Applications.Queries.GetApplicationById;
using SentinelOps.Application.Applications.Queries.GetApplications;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;
using SentinelOps.Contracts.Responses;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApplicationsController : BaseApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ApplicationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApplications(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetApplicationsQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ApplicationDto>>.SuccessResponse(result));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ApplicationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetApplicationById(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetApplicationByIdQuery(id), cancellationToken);
        if (result == null)
        {
            return NotFound(ApiResponse<ApplicationDto>.FailureResponse($"Application with ID '{id}' was not found."));
        }

        return Ok(ApiResponse<ApplicationDto>.SuccessResponse(result));
    }

    [HttpPost]
    [Authorize(Policy = "RequireOperator")]
    [ProducesResponseType(typeof(ApiResponse<ApplicationDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
        return CreatedAtAction(nameof(GetApplicationById), new { id = result.Id }, ApiResponse<ApplicationDto>.SuccessResponse(result, "Application created successfully"));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "RequireOperator")]
    [ProducesResponseType(typeof(ApiResponse<ApplicationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateApplication(
        Guid id,
        [FromBody] UpdateApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateApplicationCommand(id, request.DisplayName, request.Description, request.IsActive);
        var result = await Mediator.Send(command, cancellationToken);
        return Ok(ApiResponse<ApplicationDto>.SuccessResponse(result, "Application updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteApplication(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new DeleteApplicationCommand(id), cancellationToken);
        if (!result)
        {
            return NotFound(ApiResponse<bool>.FailureResponse($"Application with ID '{id}' was not found."));
        }

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Application deleted successfully."));
    }

    [HttpPost("{id:guid}/environments")]
    [Authorize(Policy = "RequireOperator")]
    [ProducesResponseType(typeof(ApiResponse<EnvironmentDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateEnvironment(
        Guid id,
        [FromBody] CreateEnvironmentRequest request,
        CancellationToken cancellationToken)
    {
        var envType = Enum.TryParse<EnvironmentType>(request.EnvironmentType, true, out var parsed)
            ? parsed
            : EnvironmentType.Production;

        var command = new CreateEnvironmentCommand(id, request.Name, envType);
        var result = await Mediator.Send(command, cancellationToken);
        return Ok(ApiResponse<EnvironmentDto>.SuccessResponse(result, "Environment created successfully."));
    }

    [HttpDelete("{id:guid}/environments/{environmentId:guid}")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEnvironment(
        Guid id,
        Guid environmentId,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new DeleteEnvironmentCommand(id, environmentId), cancellationToken);
        if (!result)
        {
            return NotFound(ApiResponse<bool>.FailureResponse("Environment not found."));
        }

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Environment deleted successfully."));
    }

    [HttpPost("{id:guid}/services")]
    [Authorize(Policy = "RequireOperator")]
    [ProducesResponseType(typeof(ApiResponse<ServiceDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateService(
        Guid id,
        [FromBody] CreateServiceRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateServiceCommand(id, request.Name, request.DisplayName);
        var result = await Mediator.Send(command, cancellationToken);
        return Ok(ApiResponse<ServiceDto>.SuccessResponse(result, "Service created successfully."));
    }
}
