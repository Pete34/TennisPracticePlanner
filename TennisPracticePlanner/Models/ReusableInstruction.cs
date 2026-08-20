namespace TennisPracticePlanner.Models;

public class ReusableInstruction
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Description { get; set; } = string.Empty;

    public int DurationMinutes { get; set; }

    public int? BallCount { get; set; }

    public string? YoutubeUrl { get; set; }

    public string? YoutubeStartTime { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    // Cloud-sync sharing fields. Unused/ignored in Guest Mode (local storage).
    public bool IsShared { get; set; }

    public InstructionTag? Tag { get; set; }

    public string? OwnerUid { get; set; }

    public string? OwnerDisplayName { get; set; }

    public DateTime? SharedAtUtc { get; set; }
}
