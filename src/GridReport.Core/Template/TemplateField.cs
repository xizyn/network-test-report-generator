namespace GridReport.Core.Template;

public sealed record TemplateField(string CommentId, string Name, string OriginalText, string Scope, bool IsImage = false, bool IsRequired = true);
