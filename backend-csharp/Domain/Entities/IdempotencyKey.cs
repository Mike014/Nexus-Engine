namespace NexusEngine.Api.Domain.Entities;

public class IdempotencyKey
{
    public string Key { get; set; } = string.Empty;

    public string? ResponseBody { get; set; }

    public int ResponseStatus { get; set; }

    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(24);

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}