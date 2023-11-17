using BatteryApi.Data;
using BatteryApi.Models;

namespace BatteryApi;

public static class Utilities
{
    public static Dictionary<string, string[]> IsValid(BatteryDto battery)
    {
        Dictionary<string, string[]> errors = new();

        if (battery.ChargeState is not >= 0M and <= 100M)
        {
            errors.TryAdd("battery.charge_state.errors", new[] { "Charge state cannot be less than 0 or more than 100" });
        }

        if (battery.Voltage is not >= 0M)
        {
            errors.TryAdd("battery.voltage.errors", new[] { "Voltage cannot be a negative value" });
        }

        return errors;
    }

    public static Dictionary<string, string[]> IsValid(BatteryIssueDto batteryIssue)
    {
        Dictionary<string, string[]> errors = new();

        if (string.IsNullOrWhiteSpace(batteryIssue.Description))
        {
            errors.TryAdd("battery_issue.description.errors", new[] { "Description is empty" });
        }

        if (string.IsNullOrWhiteSpace(batteryIssue.IssueType))
        {
            errors.TryAdd("battery_issue.issue_type.errors", new[] { "Issue type is empty" });
        }

        return errors;
    }
}
