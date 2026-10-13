using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SentinelOps.Application.Exceptions.Queries.GetExceptions;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;
using SentinelOps.Contracts.Responses;

namespace SentinelOps.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ExceptionsController : BaseApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ExceptionGroupDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExceptions([FromQuery] GetExceptionsRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetExceptionsQuery(request), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ExceptionGroupDto>>.Ok(result));
    }
}
