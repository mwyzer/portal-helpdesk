namespace AIHelpdesk.Application.Options;

public class AIOptions
{
    public const string SectionName = "AI";

    public string Endpoint { get; set; } = "https://api.openai.com/v1/";
    public string ApiKey { get; set; } = string.Empty;
    public string ChatModel { get; set; } = "gpt-4o-mini";
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";
    public SystemPromptOptions SystemPrompts { get; set; } = new();
    public RateLimitOptions RateLimit { get; set; } = new();
    public BudgetOptions Budget { get; set; } = new();
}

public class SystemPromptOptions
{
    public string Default { get; set; } = "You are an AI Helpdesk assistant for an internal company portal. Answer questions based ONLY on the provided context. Be concise and professional.";
    public string NoContextResponse { get; set; } = "I cannot find relevant information in the knowledge base to answer your question. Would you like me to escalate this to a human agent?";
}

public class RateLimitOptions
{
    public int MaxRequestsPerMinute { get; set; } = 30;
    public bool Enabled { get; set; } = true;
}

public class BudgetOptions
{
    public decimal DailyTokenBudget { get; set; } = 1_000_000;
    public bool EnforceBudget { get; set; } = false;
}
