using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SentinelOps.Application.Users.Commands.AdminResetPassword;
using SentinelOps.Application.Users.Commands.AssignApplicationMember;
using SentinelOps.Application.Users.Commands.RemoveApplicationMember;
using SentinelOps.Application.Users.Commands.RevokeUserSessions;
using SentinelOps.Application.Users.Commands.UpdateUserRole;
using SentinelOps.Application.Users.Commands.UpdateUserStatus;
using SentinelOps.Application.Users.Queries.GetApplicationMembers;
using SentinelOps.Application.Users.Queries.GetUserDetail;
using SentinelOps.Application.Users.Queries.GetUsers;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;
using SentinelOps.Contracts.Responses;

namespace SentinelOps.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : BaseApiController
{
    [HttpGet]
    [Authorize(Policy = "RequireOperator")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<UserDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers([FromQuery] GetUsersRequest request, CancellationToken cancellationToken)
    {
        var query = new GetUsersQuery(
            request.Search,
            request.Role,
            request.Status,
            request.FromUtc,
            request.ToUtc,
            request.Page,
            request.PageSize);

        var result = await Mediator.Send(query, cancellationToken);
        return Ok(ApiResponse<PagedResponse<UserDto>>.SuccessResponse(result));
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "RequireOperator")]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById(string id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetUserDetailQuery(id), cancellationToken);
        if (result == null)
        {
            return NotFound(ApiResponse<UserDetailDto>.FailureResponse("User not found."));
        }

        return Ok(ApiResponse<UserDetailDto>.SuccessResponse(result));
    }

    [HttpPut("{id}/roles")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateRoles(string id, [FromBody] UpdateUserRoleRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateUserRoleCommand(id, request.Roles);
        var (success, errors) = await Mediator.Send(command, cancellationToken);

        if (!success)
        {
            return BadRequest(ApiResponse<bool>.FailureResponse(string.Join("; ", errors)));
        }

        return Ok(ApiResponse<bool>.SuccessResponse(true, "User roles updated successfully."));
    }

    [HttpPut("{id}/status")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateUserStatusRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateUserStatusCommand(id, request.IsActive, request.IsLocked, request.LockoutDurationMinutes);
        var (success, errors) = await Mediator.Send(command, cancellationToken);

        if (!success)
        {
            return BadRequest(ApiResponse<bool>.FailureResponse(string.Join("; ", errors)));
        }

        return Ok(ApiResponse<bool>.SuccessResponse(true, "User status updated successfully."));
    }

    [HttpPost("{id}/revoke-sessions")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RevokeSessions(string id, CancellationToken cancellationToken)
    {
        var command = new RevokeUserSessionsCommand(id);
        var (success, errors) = await Mediator.Send(command, cancellationToken);

        if (!success)
        {
            return BadRequest(ApiResponse<bool>.FailureResponse(string.Join("; ", errors)));
        }

        return Ok(ApiResponse<bool>.SuccessResponse(true, "All user sessions and tokens revoked successfully."));
    }

    [HttpPost("{id}/reset-password")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(string id, [FromBody] AdminResetPasswordRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
        {
            return BadRequest(ApiResponse<bool>.FailureResponse("Password must be at least 8 characters."));
        }

        if (request.NewPassword != request.ConfirmPassword)
        {
            return BadRequest(ApiResponse<bool>.FailureResponse("Passwords do not match."));
        }

        var command = new AdminResetPasswordCommand(id, request.NewPassword);
        var (success, errors) = await Mediator.Send(command, cancellationToken);

        if (!success)
        {
            return BadRequest(ApiResponse<bool>.FailureResponse(string.Join("; ", errors)));
        }

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Password reset successfully."));
    }

    [HttpGet("applications/{applicationId:guid}/members")]
    [Authorize(Policy = "RequireOperator")]
    [ProducesResponseType(typeof(ApiResponse<List<ApplicationMemberDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApplicationMembers(Guid applicationId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetApplicationMembersQuery(applicationId), cancellationToken);
        return Ok(ApiResponse<List<ApplicationMemberDto>>.SuccessResponse(result));
    }

    [HttpPost("applications/{applicationId:guid}/members")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AssignApplicationMember(
        Guid applicationId,
        [FromBody] AssignApplicationMemberRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AssignApplicationMemberCommand(applicationId, request.UserId, request.Role);
        var (success, error) = await Mediator.Send(command, cancellationToken);

        if (!success)
        {
            return BadRequest(ApiResponse<bool>.FailureResponse(error ?? "Failed to assign member."));
        }

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Application membership assigned successfully."));
    }

    [HttpDelete("applications/{applicationId:guid}/members/{userId}")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RemoveApplicationMember(
        Guid applicationId,
        string userId,
        CancellationToken cancellationToken)
    {
        var command = new RemoveApplicationMemberCommand(applicationId, userId);
        var (success, error) = await Mediator.Send(command, cancellationToken);

        if (!success)
        {
            return BadRequest(ApiResponse<bool>.FailureResponse(error ?? "Failed to remove member."));
        }

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Application member removed successfully."));
    }
}
