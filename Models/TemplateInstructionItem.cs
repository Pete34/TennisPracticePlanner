namespace TennisPracticePlanner.Models;

public class TemplateInstructionItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string? SourceInstructionId { get; set; }

    public string Description { get; set; } = string.Empty;

    public int DurationMinutes { get; set; }

    public int? BallCount { get; set; }

    public string? YoutubeUrl { get; set; }

    public string? YoutubeStartTime { get; set; }

    public int SortOrder { get; set; }

    public static TemplateInstructionItem FromReusableInstruction(ReusableInstruction instruction, int sortOrder)
    {
        return new TemplateInstructionItem
        {
            SourceInstructionId = instruction.Id,
            Description = instruction.Description,
            DurationMinutes = instruction.DurationMinutes,
            BallCount = instruction.BallCount,
            YoutubeUrl = instruction.YoutubeUrl,
            YoutubeStartTime = instruction.YoutubeStartTime,
            SortOrder = sortOrder
        };
    }
}
