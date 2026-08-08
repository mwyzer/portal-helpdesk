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
/// re-derives the caller's identity from IHttpContextAccessor and passes it to ITicketService,
/// which now enforces ticket-level ownership itself (submitter, assigned employee/agent, or
/// Agent/Manager/Super Admin) -- previously this class carried its own duplicate access check
/// because the service didn't enforce one at all.
/// </summary>
[McpServerToolType]
public class TicketMcpTools
{
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

    private static bool IsPrivileged(ClaimsPrincipal user) =>
        user.IsInRole("Agent") || user.IsInRole("Manager") || user.IsInRole("Super Admin");

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
        try
        {
            return await _tickets.GetByIdAsync(ticketId, CallerId(user), IsPrivileged(user));
        }
        catch (UnauthorizedAccessException)
        {
            // Same message whether the ticket doesn't exist or the caller can't see it, so this can't
            // be used to enumerate other users' ticket ids by observing a different error for each case.
            throw new McpException("Ticket not found");
        }
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
        try
        {
            return await _tickets.UpdateAsync(ticketId, CallerId(user), IsPrivileged(user),
                new UpdateTicketRequest(title, description, subCategory, priority));
        }
        catch (UnauthorizedAccessException)
        {
            throw new McpException("Ticket not found");
        }
    }

    [McpServerTool, Description("Get the SLA deadline and status for a ticket. Same visibility rule as get_ticket.")]
    public async Task<string> GetSla(Guid ticketId)
    {
        var user = CallerOrThrow();
        TicketDetailResponse ticket;
        try
        {
            ticket = await _tickets.GetByIdAsync(ticketId, CallerId(user), IsPrivileged(user));
        }
        catch (UnauthorizedAccessException)
        {
            throw new McpException("Ticket not found");
        }

        return System.Text.Json.JsonSerializer.Serialize(new
        {
            ticket.Id,
            ticket.SLADeadline,
            ticket.SLAStatus
        });
    }
}
