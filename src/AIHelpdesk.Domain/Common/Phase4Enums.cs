namespace AIHelpdesk.Domain.Common;

public enum KnowledgeDocumentStatus
{
    Pending,
    Indexing,
    Ready,
    Failed
}

public enum ChatSessionStatus
{
    Active,
    Resolved,
    Escalated
}

public enum ChatMessageRole
{
    User,
    Assistant,
    System
}
