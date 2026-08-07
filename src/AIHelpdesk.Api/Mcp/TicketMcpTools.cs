using System.ComponentModel;
using System.Security.Claims;
using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Tickets;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace AIHelpdesk.Api.Mcp;

/// <summary>
/// MCP tools for the Ticket Agent. Hosted behind the same JWT bearer auth as the rest of the API
/// (see Program.cs: app.MapMcp("/mcp").RequireAuthorization()), but MCP tool invocation doesn't go
/// through [Authorize(Roles=...)] action filters the way TicketsController does -- every tool here
/// re-derives the caller's identity from IHttpContextAccessor and enforces ticket ownership itself.
///
/// Note: TicketsController.GetById/Update currently have NO ownership check at the REST layer (any
/// authenticated user can read/edit any ticket by id -- a pre-existing gap, separate from this
/// work). The scoping below is intentionally stricter than that controller so an LLM-driven agent
/// can't be used to read or edit another employee's ticket, even though the REST endpoint itself
/// would currently allow it.
/// </summary>
[McpServerToolType]
public class TicketMcpTools
{
    private static readonly string[] StaffRoles = ["Agent", "Manager", "Super Admin"];

    private readonly ITicketService _tickets;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TicketMcpTools(ITicketService tickets, IHttpContextAccessor httpContextAccessor)
    {
        _tickets = tickets;
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal CallerOrThrow() =>
        _httpContextAccessor.HttpContext?.User
            ?? throw new McpException("No authenticated caller for this request");

    private static Guid CallerId(ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static bool CanAccess(ClaimsPrincipal user, Guid callerId, TicketDetailResponse ticket) =>
        ticket.SubmittedById == callerId
        || ticket.AssignedAgentId == callerId
        || StaffRoles.Any(user.IsInRole);

    [McpServerTool, Description("Create a new support ticket on behalf of the current user.")]
    public async Task<TicketDetailResponse> CreateTicket(
        Guid categoryId,
        [Description("Short summary of the issue")] string title,
        [Description("Full description of the issue")] string description,
        string? subCategory = null,
        [Description("Low, Medium, High, or Critical")] string? priority = null)
    {
        var user = CallerOrThrow();
        return await _tickets.CreateAsync(CallerId(user),
            new CreateTicketRequest(categoryId, title, description, subCategory, priority));
    }

    [McpServerTool, Description("Get a ticket by id. Only visible to its submitter, its assigned agent, or staff (Agent/Manager/Super Admin).")]
    public async Task<TicketDetailResponse> GetTicket(Guid ticketId)
    {
        var user = CallerOrThrow();
        var callerId = CallerId(user);
        var ticket = await _tickets.GetByIdAsync(ticketId);

        // Same message whether the ticket doesn't exist or the caller can't see it, so this can't
        // be used to enumerate other users' ticket ids by observing a different error for each case.
        if (!CanAccess(user, callerId, ticket))
            throw new McpException("Ticket not found");

        return ticket;
    }

    [McpServerTool, Description("Update a ticket's title, description, sub-category, or priority. Only the submitter, assigned agent, or staff may update it.")]
    public async Task<TicketDetailResponse> UpdateTicket(
        Guid ticketId,
        string title,
        string description,
        string? subCategory = null,
        string? priority = null)
    {
        var user = CallerOrThrow();
        var callerId = CallerId(user);
        var existing = await _tickets.GetByIdAsync(ticketId);

        if (!CanAccess(user, callerId, existing))
            throw new McpException("Ticket not found");

        return await _tickets.UpdateAsync(ticketId, new UpdateTicketRequest(title, description, subCategory, priority));
    }

    [McpServerTool, Description("Get the SLA deadline and status for a ticket. Same visibility rule as get_ticket.")]
    public async Task<string> GetSla(Guid ticketId)
    {
        var user = CallerOrThrow();
        var callerId = CallerId(user);
        var ticket = await _tickets.GetByIdAsync(ticketId);

        if (!CanAccess(user, callerId, ticket))
            throw new McpException("Ticket not found");

        return System.Text.Json.JsonSerializer.Serialize(new
        {
            ticket.Id,
            ticket.SLADeadline,
            ticket.SLAStatus
        });
    }
}
