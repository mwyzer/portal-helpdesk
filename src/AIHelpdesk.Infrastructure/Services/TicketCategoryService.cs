using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Tickets;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdesk.Infrastructure.Services;

public class TicketCategoryService : ITicketCategoryService
{
    private readonly ApplicationDbContext _context;

    public TicketCategoryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IList<TicketCategoryResponse>> GetAllAsync(Guid? departmentId)
    {
        var query = _context.TicketCategories.AsQueryable();
        if (departmentId.HasValue)
            query = query.Where(c => c.DepartmentId == null || c.DepartmentId == departmentId.Value);

        return await query
            .OrderBy(c => c.Name)
            .Select(c => new TicketCategoryResponse(
                c.Id, c.Name, c.Description, c.DefaultPriority.ToString(),
                c.SLAHours, c.DepartmentId, c.Department != null ? c.Department.Name : null,
                c.CreatedAt))
            .ToListAsync();
    }

    public async Task<TicketCategoryResponse> GetByIdAsync(Guid id)
    {
        var c = await _context.TicketCategories
            .Include(x => x.Department)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException("Ticket category not found");

        return new TicketCategoryResponse(
            c.Id, c.Name, c.Description, c.DefaultPriority.ToString(),
            c.SLAHours, c.DepartmentId, c.Department?.Name, c.CreatedAt);
    }

    public async Task<TicketCategoryResponse> CreateAsync(CreateTicketCategoryRequest request)
    {
        var category = new TicketCategory
        {
            Name = request.Name,
            Description = request.Description,
            DefaultPriority = Enum.TryParse<Domain.Common.TicketPriority>(request.DefaultPriority, true, out var p) ? p : Domain.Common.TicketPriority.Normal,
            SLAHours = request.SLAHours,
            DepartmentId = request.DepartmentId
        };

        _context.TicketCategories.Add(category);
        await _context.SaveChangesAsync();

        return new TicketCategoryResponse(
            category.Id, category.Name, category.Description, category.DefaultPriority.ToString(),
            category.SLAHours, category.DepartmentId, null, category.CreatedAt);
    }

    public async Task<TicketCategoryResponse> UpdateAsync(Guid id, UpdateTicketCategoryRequest request)
    {
        var category = await _context.TicketCategories.FindAsync(id)
            ?? throw new KeyNotFoundException("Ticket category not found");

        category.Name = request.Name;
        category.Description = request.Description;
        category.DefaultPriority = Enum.TryParse<Domain.Common.TicketPriority>(request.DefaultPriority, true, out var pri) ? pri : Domain.Common.TicketPriority.Normal;
        category.SLAHours = request.SLAHours;
        category.DepartmentId = request.DepartmentId;
        category.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new TicketCategoryResponse(
            category.Id, category.Name, category.Description, category.DefaultPriority.ToString(),
            category.SLAHours, category.DepartmentId, null, category.CreatedAt);
    }

    public async Task DeleteAsync(Guid id)
    {
        var category = await _context.TicketCategories.FindAsync(id)
            ?? throw new KeyNotFoundException("Ticket category not found");

        category.IsDeleted = true;
        category.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
}
