namespace BatteryApi.Models;

public class Battery
{
    public int Id { get; set; }
    public decimal ChargeState { get; set; }
    public decimal Voltage { get; set; }
    public BatteryHealth Health { get; set; }
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
    public int DailyHealthCounter { get; set; }

    public ICollection<BatteryIssue> BatteryIssues { get; }
}

public enum BatteryHealth
{
    Bad,
    Good,
    VeryGood,
    Excellent,
}
