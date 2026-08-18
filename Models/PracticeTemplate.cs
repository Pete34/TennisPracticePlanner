namespace TennisPracticePlanner.Models;

public class PracticeTemplate
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Title { get; set; } = string.Empty;

    public List<TemplateInstructionItem> InstructionItems { get; set; } = [];

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}
