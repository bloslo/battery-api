namespace BatteryApi.Models;

public class BatteryIssue
{
    public int Id { get; set; }
    public string? IssueType { get; set; }
    public string? Description { get; set; }
    public DateTime Occurred { get; set; }

    public int BatteryId { get; set; }
}
