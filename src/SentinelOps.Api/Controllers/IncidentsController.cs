using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Application.Common.Mappings;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;
using SentinelOps.Contracts.Responses;
using SentinelOps.Domain.Entities;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/incidents")]
public class IncidentsController : BaseApiController
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public IncidentsController(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<IncidentDto>>>> GetIncidents(
        [FromQuery] Guid? applicationId,
        [FromQuery] string? status,
        [FromQuery] string? severity,
        [FromQuery] string? serviceName,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Incidents
            .Include(i => i.TimelineEvents)
            .Include(i => i.Comments)
            .AsNoTracking()
            .AsQueryable();

        if (applicationId.HasValue && applicationId.Value != Guid.Empty)
        {
            query = query.Where(i => i.ApplicationId == applicationId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<IncidentStatus>(status, true, out var incidentStatus))
        {
            query = query.Where(i => i.Status == incidentStatus);
        }

        if (!string.IsNullOrWhiteSpace(severity) && Enum.TryParse<Severity>(severity, true, out var incidentSeverity))
        {
            query = query.Where(i => i.Severity == incidentSeverity);
        }

        if (!string.IsNullOrWhiteSpace(serviceName))
        {
            query = query.Where(i => i.ServiceName == serviceName);
        }

        int totalCount = await query.CountAsync();

        var incidents = await query
            .OrderByDescending(i => i.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = incidents.Select(i => i.ToDto()).ToList();
        var paged = PagedResponse<IncidentDto>.Create(dtos, totalCount, page, pageSize);

        return Ok(ApiResponse<PagedResponse<IncidentDto>>.SuccessResponse(paged));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<IncidentDetailDto>>> GetIncident(Guid id)
    {
        var incident = await _context.Incidents
            .Include(i => i.TimelineEvents)
            .Include(i => i.Comments)
            .Include(i => i.AlertRelations)
                .ThenInclude(r => r.AlertRule)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id);

        if (incident == null)
        {
            return NotFound(ApiResponse<IncidentDetailDto>.FailureResponse("Incident not found."));
        }

        return Ok(ApiResponse<IncidentDetailDto>.SuccessResponse(incident.ToDetailDto()));
    }

    [HttpPost]
    [Authorize(Policy = "RequireOperator")]
    public async Task<ActionResult<ApiResponse<IncidentDto>>> CreateIncident([FromBody] CreateIncidentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(ApiResponse<IncidentDto>.FailureResponse("Incident title is required."));
        }

        var app = await _context.Applications.FindAsync(request.ApplicationId);
        if (app == null)
        {
            return BadRequest(ApiResponse<IncidentDto>.FailureResponse("Application not found."));
        }

        if (!Enum.TryParse<Severity>(request.Severity, true, out var severity))
        {
            severity = Severity.Error;
        }

        var incidentNumber = $"INC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        var incident = new Incident
        {
            IncidentNumber = incidentNumber,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            Severity = severity,
            Status = IncidentStatus.Open,
            ApplicationId = request.ApplicationId,
            EnvironmentName = request.EnvironmentName?.Trim(),
            ServiceName = request.ServiceName?.Trim(),
            AssignedToUserId = request.AssignedToUserId?.Trim(),
            AssignedToUserName = request.AssignedToUserName?.Trim(),
            RelatedTraceId = request.RelatedTraceId?.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        var actorId = _currentUserService.UserId ?? "usr_operator";
        var actorName = _currentUserService.UserName ?? "Operator";

        incident.TimelineEvents.Add(new IncidentTimelineEvent
        {
            IncidentId = incident.Id,
            EventType = IncidentTimelineEventType.Created,
            TimestampUtc = DateTime.UtcNow,
            ActorId = actorId,
            ActorName = actorName,
            Title = "Incident Created",
            Description = $"Created manually by {actorName}."
        });

        if (request.RelatedAlertRuleId.HasValue && request.RelatedAlertRuleId.Value != Guid.Empty)
        {
            incident.AlertRelations.Add(new IncidentAlertRelation
            {
                IncidentId = incident.Id,
                AlertRuleId = request.RelatedAlertRuleId.Value,
                LinkedAtUtc = DateTime.UtcNow
            });
        }

        _context.Incidents.Add(incident);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetIncident), new { id = incident.Id }, ApiResponse<IncidentDto>.SuccessResponse(incident.ToDto(), "Incident created successfully."));
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Policy = "RequireOperator")]
    public async Task<ActionResult<ApiResponse<IncidentDto>>> UpdateIncident(Guid id, [FromBody] UpdateIncidentRequest request)
    {
        var incident = await _context.Incidents
            .Include(i => i.TimelineEvents)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (incident == null)
        {
            return NotFound(ApiResponse<IncidentDto>.FailureResponse("Incident not found."));
        }

        var actorId = _currentUserService.UserId ?? "usr_operator";
        var actorName = _currentUserService.UserName ?? "Operator";

        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            incident.Title = request.Title.Trim();
        }

        if (request.Description != null)
        {
            incident.Description = request.Description.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.Severity) && Enum.TryParse<Severity>(request.Severity, true, out var newSeverity))
        {
            incident.ChangeSeverity(newSeverity, actorId, actorName);
        }

        await _context.SaveChangesAsync();
        return Ok(ApiResponse<IncidentDto>.SuccessResponse(incident.ToDto(), "Incident updated successfully."));
    }

    [HttpPost("{id:guid}/assign")]
    [Authorize(Policy = "RequireOperator")]
    public async Task<ActionResult<ApiResponse<IncidentDto>>> AssignIncident(Guid id, [FromBody] AssignIncidentRequest request)
    {
        var incident = await _context.Incidents
            .Include(i => i.TimelineEvents)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (incident == null)
        {
            return NotFound(ApiResponse<IncidentDto>.FailureResponse("Incident not found."));
        }

        if (string.IsNullOrWhiteSpace(request.UserId) || string.IsNullOrWhiteSpace(request.UserName))
        {
            return BadRequest(ApiResponse<IncidentDto>.FailureResponse("User ID and User Name are required."));
        }

        var actorId = _currentUserService.UserId ?? "usr_operator";
        var actorName = _currentUserService.UserName ?? "Operator";

        incident.Assign(request.UserId.Trim(), request.UserName.Trim(), actorId, actorName);
        var lastEvent = incident.TimelineEvents.Last();
        _context.IncidentTimelineEvents.Add(lastEvent);
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<IncidentDto>.SuccessResponse(incident.ToDto(), $"Assigned to {request.UserName}."));
    }

    [HttpPost("{id:guid}/transition")]
    [Authorize(Policy = "RequireOperator")]
    public async Task<ActionResult<ApiResponse<IncidentDto>>> TransitionIncident(Guid id, [FromBody] TransitionIncidentRequest request)
    {
        var incident = await _context.Incidents
            .Include(i => i.TimelineEvents)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (incident == null)
        {
            return NotFound(ApiResponse<IncidentDto>.FailureResponse("Incident not found."));
        }

        if (!Enum.TryParse<IncidentStatus>(request.TargetStatus, true, out var targetStatus))
        {
            return BadRequest(ApiResponse<IncidentDto>.FailureResponse($"Invalid target status: {request.TargetStatus}."));
        }

        var actorId = _currentUserService.UserId ?? "usr_operator";
        var actorName = _currentUserService.UserName ?? "Operator";

        try
        {
            switch (targetStatus)
            {
                case IncidentStatus.Investigating:
                    incident.Investigate(actorId, actorName);
                    break;
                case IncidentStatus.Mitigated:
                    incident.Mitigate(actorId, actorName, request.ResolutionNotes);
                    break;
                case IncidentStatus.Resolved:
                    if (string.IsNullOrWhiteSpace(request.RootCause))
                    {
                        return BadRequest(ApiResponse<IncidentDto>.FailureResponse("Root cause summary is required to resolve an incident."));
                    }
                    incident.Resolve(actorId, actorName, request.RootCause.Trim(), request.ResolutionNotes?.Trim() ?? string.Empty);
                    break;
                case IncidentStatus.Closed:
                    incident.Close(actorId, actorName, request.ClosingNotes?.Trim());
                    break;
                case IncidentStatus.Open:
                    incident.Reopen(actorId, actorName, request.Reason?.Trim() ?? "Reopened by operator");
                    break;
                default:
                    return BadRequest(ApiResponse<IncidentDto>.FailureResponse($"Unsupported status transition to {targetStatus}."));
            }

            var lastEvent = incident.TimelineEvents.LastOrDefault();
            if (lastEvent != null)
            {
                _context.IncidentTimelineEvents.Add(lastEvent);
            }

            await _context.SaveChangesAsync();
            return Ok(ApiResponse<IncidentDto>.SuccessResponse(incident.ToDto(), $"Incident transitioned to {targetStatus}."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<IncidentDto>.FailureResponse(ex.Message));
        }
    }

    [HttpPost("{id:guid}/comments")]
    [Authorize(Policy = "RequireViewer")]
    public async Task<ActionResult<ApiResponse<IncidentDetailDto>>> AddComment(Guid id, [FromBody] AddIncidentCommentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CommentText))
        {
            return BadRequest(ApiResponse<IncidentDetailDto>.FailureResponse("Comment text cannot be empty."));
        }

        var incident = await _context.Incidents
            .Include(i => i.TimelineEvents)
            .Include(i => i.Comments)
            .Include(i => i.AlertRelations)
                .ThenInclude(r => r.AlertRule)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (incident == null)
        {
            return NotFound(ApiResponse<IncidentDetailDto>.FailureResponse("Incident not found."));
        }

        var actorId = _currentUserService.UserId ?? "usr_user";
        var actorName = _currentUserService.UserName ?? "User";

        incident.AddComment(actorId, actorName, request.CommentText.Trim());
        var lastComment = incident.Comments.Last();
        var lastEvent = incident.TimelineEvents.Last();
        _context.IncidentComments.Add(lastComment);
        _context.IncidentTimelineEvents.Add(lastEvent);

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<IncidentDetailDto>.SuccessResponse(incident.ToDetailDto(), "Comment added."));
    }
}
