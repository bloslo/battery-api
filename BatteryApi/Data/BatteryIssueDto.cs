namespace BatteryApi.Models;

public class BatteryIssueDto
{
    public string? IssueType { get; set; }
    public string? Description { get; set; }
    public DateTime Occurred { get; set; }
}
