using AIHelpdesk.Domain.Common;

namespace AIHelpdesk.Domain.Entities;

public class DocumentTemplate : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ContentTemplate { get; set; } = string.Empty;
    public string Variables { get; set; } = "[]";
    public bool IsActive { get; set; } = true;
}
