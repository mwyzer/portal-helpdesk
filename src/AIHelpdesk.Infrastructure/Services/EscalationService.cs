using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Tickets;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdesk.Infrastructure.Services;

public class EscalationService : IEscalationService
{
    private readonly ApplicationDbContext _context;

    public EscalationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<EscalationResponse>> GetEscalationsAsync(Guid? departmentId, int page, int pageSize, string? status)
    {
        var query = _context.Escalations.AsQueryable();

        if (departmentId.HasValue)
            query = query.Where(e => e.Ticket.DepartmentId == departmentId.Value);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<Domain.Common.EscalationStatus>(status, true, out var s))
            query = query.Where(e => e.Status == s);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(e => e.EscalatedBy)
            .Include(e => e.AssignedTo)
            .Select(e => new EscalationResponse(
                e.Id, e.TicketId, e.EscalatedById, e.EscalatedBy.FullName,
                e.AssignedToId, e.AssignedTo != null ? e.AssignedTo.FullName : null,
                e.Reason, e.Status.ToString(), e.ResolvedAt, e.CreatedAt))
            .ToListAsync();

        return new PagedResult<EscalationResponse>(items, totalCount, page, pageSize);
    }

    public async Task<IList<EscalationResponse>> GetPendingAsync(Guid departmentId)
    {
        return await _context.Escalations
            .Where(e => e.Ticket.DepartmentId == departmentId && e.Status == Domain.Common.EscalationStatus.Pending)
            .Include(e => e.EscalatedBy)
            .Include(e => e.Ticket)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new EscalationResponse(
                e.Id, e.TicketId, e.EscalatedById, e.EscalatedBy.FullName,
                e.AssignedToId, null, e.Reason, e.Status.ToString(), e.ResolvedAt, e.CreatedAt))
            .ToListAsync();
    }

    public async Task<EscalationResponse> CreateAsync(Guid ticketId, Guid escalatedById, CreateEscalationRequest request)
    {
        var escalation = new Escalation
        {
            TicketId = ticketId,
            EscalatedById = escalatedById,
            AssignedToId = request.AssignedToId,
            Reason = request.Reason,
            Status = Domain.Common.EscalationStatus.Pending
        };

        _context.Escalations.Add(escalation);

        // Mark ticket as escalated
        var ticket = await _context.Tickets.FindAsync(ticketId);
        if (ticket != null)
        {
            ticket.EscalatedAt = DateTime.UtcNow;
            ticket.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        await _context.Entry(escalation).Reference(e => e.EscalatedBy).LoadAsync();

        return new EscalationResponse(
            escalation.Id, escalation.TicketId, escalation.EscalatedById,
            escalation.EscalatedBy.FullName, escalation.AssignedToId, null,
            escalation.Reason, escalation.Status.ToString(), escalation.ResolvedAt, escalation.CreatedAt);
    }

    public async Task AcceptAsync(Guid id, Guid userId)
    {
        var escalation = await _context.Escalations.FindAsync(id)
            ?? throw new KeyNotFoundException("Escalation not found");

        escalation.Status = Domain.Common.EscalationStatus.Accepted;
        escalation.AssignedToId = userId;
        escalation.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task ResolveAsync(Guid id, Guid userId)
    {
        var escalation = await _context.Escalations.FindAsync(id)
            ?? throw new KeyNotFoundException("Escalation not found");

        escalation.Status = Domain.Common.EscalationStatus.Resolved;
        escalation.ResolvedAt = DateTime.UtcNow;
        escalation.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task DeclineAsync(Guid id, Guid userId)
    {
        var escalation = await _context.Escalations.FindAsync(id)
            ?? throw new KeyNotFoundException("Escalation not found");

        escalation.Status = Domain.Common.EscalationStatus.Declined;
        escalation.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }
}
