namespace PGLLMS.Portal.API.DTOs;

public record ChatRequest(string Question, List<ChatHistoryMessage>? History = null);

public record ChatHistoryMessage(string Role, string Content);

public record ChatResponse(string Answer, List<ChatSource> Sources);

public record ChatSource(string SourceId, string Title, string Type);
