using System.Net;
using System.Net.Http.Json;
using BatteryApi.Data;
using BatteryApi.Models;
using BatteryApi.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace BatteryApi.Tests;

public class BatteryApiTests : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _httpClient;

    public BatteryApiTests(TestWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _httpClient = factory.CreateClient();
    }

    public static IEnumerable<object[]> InvalidBatteries => new List<object[]>
    {
        new object[] { new BatteryDto { ChargeState = -20M, Voltage = 25.5M }, "Charge state cannot be less than 0 or more than 100" },
        new object[] { new BatteryDto { ChargeState = 80M, Voltage = -12.5M }, "Voltage cannot be a negative value" }
    };

    public static IEnumerable<object[]> ValidBatteries => new List<object[]>
    {
        new object[]
        {
            new List<BatteryDto>
            {
                new BatteryDto { ChargeState = 65.4M, Voltage = 275M },
                new BatteryDto { ChargeState = 73M, Voltage = 300M },
                new BatteryDto { ChargeState = 47.3M, Voltage = 450M }
            }
        }
    };

    public static IEnumerable<object[]> ValidBatteryIssues => new List<object[]>
    {
        new object[]
        {
            new List<BatteryIssueDto>
            {
                new BatteryIssueDto
                {
                    IssueType = "Failure",
                    Description = "Battery stopped charging.",
                    Occurred = DateTime.UtcNow.AddDays(-1)
                },
                new BatteryIssueDto
                {
                    IssueType = "Rapidly Discharging",
                    Description = "Battery is discharging too fast.",
                    Occurred = DateTime.UtcNow.AddDays(-6)
                },
                new BatteryIssueDto
                {
                    IssueType = "Sensor Fault",
                    Description = "Battery voltage is not reported correctly.",
                    Occurred = DateTime.UtcNow.AddDays(-10)
                }
            }
        }
    };

    public static IEnumerable<object[]> InValidBatteryIssues => new List<object[]>
    {
        new object[]
        {
            new BatteryIssueDto
            {
                IssueType = "",
                Description = "Battery stopped charging.",
                Occurred = DateTime.UtcNow.AddDays(-1)
            },
            "Issue type is empty"
        },
        new object[]
        {
            new BatteryIssueDto
            {
                IssueType = "Rapidly Discharging",
                Description = "",
                Occurred = DateTime.UtcNow.AddDays(-6)
            },
            "Description is empty"
        }
    };

    [Theory]
    [MemberData(nameof(InvalidBatteries))]
    public async Task PostBatteryWithValidationProblems(BatteryDto battery, string errorMessage)
    {
        var response = await _httpClient.PostAsJsonAsync("/batteries", battery);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problemResult = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();

        Assert.NotNull(problemResult?.Errors);
        Assert.Collection(problemResult.Errors, (error) => Assert.Equal(errorMessage, error.Value.First()));
    }

    [Fact]
    public async Task PostBatteryWithValidParameters()
    {
        await CleanUpDb();

        var response = await _httpClient.PostAsJsonAsync("/batteries", new BatteryDto
        {
            ChargeState = 65M,
            Voltage = 125.6M,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var batteries = await _httpClient.GetFromJsonAsync<List<Battery>>("/batteries");

        Assert.NotNull(batteries);
        Assert.Single(batteries);

        Assert.Collection(batteries, (battery) =>
        {
            Assert.Equal(65M, battery.ChargeState);
            Assert.Equal(BatteryHealth.Excellent, battery.Health);
            Assert.Equal(125.6M, battery.Voltage);
            Assert.Equal(0, battery.DailyHealthCounter);
        });
    }

    [Theory]
    [MemberData(nameof(ValidBatteries))]
    public async Task GetAllBatteries(List<BatteryDto> data)
    {
        await CleanUpDb();
        foreach (BatteryDto newBattery in data)
        {
            var response = await _httpClient.PostAsJsonAsync("/batteries", newBattery);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }
        var batteries = await _httpClient.GetFromJsonAsync<List<Battery>>("/batteries");

        Assert.NotNull(batteries);
        Assert.NotEmpty(batteries);
        Assert.Equal(3, batteries.Count);
    }

    [Fact]
    public async Task GetBattery()
    {
        await CleanUpDb();

        var response = await _httpClient.PostAsJsonAsync("/batteries", new BatteryDto
        { 
            ChargeState = 42M, Voltage = 125M
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var createdBattery = await response.Content.ReadFromJsonAsync<Battery>();
        Assert.NotNull(createdBattery);

        var battery = await _httpClient.GetFromJsonAsync<Battery>($"/batteries/{createdBattery.Id}");
        Assert.NotNull(battery);
        Assert.Equal(42M, battery.ChargeState);
        Assert.Equal(125M, battery.Voltage);
        Assert.Equal(BatteryHealth.Excellent, battery.Health);
        Assert.Equal(DateTime.Now.Date, battery.Created.Date);
        Assert.Equal(DateTime.Now.Date, battery.Updated.Date);
    }

    [Fact]
    public async Task UpdateBattery()
    {
        await CleanUpDb();
        var response = await _httpClient.PostAsJsonAsync("/batteries", new BatteryDto
        { 
            ChargeState = 65.4M, Voltage = 275M
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var createdBattery = await response.Content.ReadFromJsonAsync<Battery>();
        Assert.NotNull(createdBattery);

        var updateResponse = await _httpClient.PutAsJsonAsync($"/batteries/{createdBattery.Id}",
            new BatteryDto
            {
                ChargeState = 100M, Voltage = 275M,
            });
        Assert.Equal(HttpStatusCode.Created, updateResponse.StatusCode);
        var updatedBattery = await updateResponse.Content.ReadFromJsonAsync<Battery>();

        Assert.NotNull(updatedBattery);
        Assert.Equal(100M, updatedBattery.ChargeState);
        Assert.Equal(275M, updatedBattery.Voltage);
        Assert.Equal(BatteryHealth.Excellent, updatedBattery.Health);
        Assert.Equal(1, updatedBattery.DailyHealthCounter);
    }

    [Fact]
    public async Task RemoveBattery()
    {
        await CleanUpDb();
        var response = await _httpClient.PostAsJsonAsync("/batteries", new BatteryDto
        { 
            ChargeState = 65.4M, Voltage = 275M
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var createdBattery = await response.Content.ReadFromJsonAsync<Battery>();
        Assert.NotNull(createdBattery);

        var deleteResponse = await _httpClient.DeleteAsync($"/batteries/{createdBattery.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _httpClient.GetAsync($"/batteries/{createdBattery.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Theory]
    [MemberData(nameof(ValidBatteryIssues))]
    public async Task CreateBatteryIssues(List<BatteryIssueDto> batteryIssues)
    {
        await CleanUpDb();
        var response = await _httpClient.PostAsJsonAsync("/batteries", new BatteryDto
        { 
            ChargeState = 49M, Voltage = 365.5M
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var createdBattery = await response.Content.ReadFromJsonAsync<Battery>();
        Assert.NotNull(createdBattery);

        foreach (var issue in batteryIssues)
        {
            var batteryIssueResp = await _httpClient.PostAsJsonAsync($"/batteries/{createdBattery.Id}/issues", issue);
            Assert.Equal(HttpStatusCode.Created, batteryIssueResp.StatusCode);
        }
        

        var issues = await _httpClient.GetFromJsonAsync<List<BatteryIssue>>($"/batteries/{createdBattery.Id}/issues");

        Assert.NotNull(issues);
        Assert.Equal(3, issues.Count);
        Assert.All(issues, issue => 
            Assert.Equal(createdBattery.Id, issue.BatteryId)
        );
    }

    [Fact]
    public async Task UpdateBatteryIssue()
    {
        await CleanUpDb();
        var response = await _httpClient.PostAsJsonAsync("/batteries", new BatteryDto
        { 
            ChargeState = 49M, Voltage = 365.5M
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var createdBattery = await response.Content.ReadFromJsonAsync<Battery>();
        Assert.NotNull(createdBattery);

        var batteryIssueResp = await _httpClient.PostAsJsonAsync($"/batteries/{createdBattery.Id}/issues",
            new BatteryIssueDto
            {
                IssueType = "Failure",
                Description = "Battery stopped charging.",
                Occurred = DateTime.UtcNow.AddDays(-1)
            });
        Assert.Equal(HttpStatusCode.Created, batteryIssueResp.StatusCode);

        var issue = await batteryIssueResp.Content.ReadFromJsonAsync<BatteryIssue>();
        Assert.NotNull(issue);

        var updateResponse = await _httpClient.PutAsJsonAsync($"/batteries/{createdBattery.Id}/issues/{issue.Id}",
            new BatteryIssueDto
            {
                IssueType = "Malfunction",
                Description = issue.Description,
                Occurred = issue.Occurred
            });
        Assert.Equal(HttpStatusCode.Created, updateResponse.StatusCode);
    }

    [Fact]
    public async Task RemoveBatteryIssue()
    {
        await CleanUpDb();
        var response = await _httpClient.PostAsJsonAsync("/batteries", new BatteryDto
        { 
            ChargeState = 49M, Voltage = 365.5M
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var createdBattery = await response.Content.ReadFromJsonAsync<Battery>();
        Assert.NotNull(createdBattery);

        var batteryIssueResp = await _httpClient.PostAsJsonAsync($"/batteries/{createdBattery.Id}/issues",
            new BatteryIssueDto
            {
                IssueType = "Failure",
                Description = "Battery stopped charging.",
                Occurred = DateTime.UtcNow.AddDays(-1)
            });
        Assert.Equal(HttpStatusCode.Created, batteryIssueResp.StatusCode);

        var issue = await batteryIssueResp.Content.ReadFromJsonAsync<BatteryIssue>();
        Assert.NotNull(issue);

        var deleteResponse = await _httpClient.DeleteAsync($"/batteries/{issue.BatteryId}/issues/{issue.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Theory]
    [MemberData(nameof(InValidBatteryIssues))]
    public async Task PostBatteryIssuesWithValidationProblems(BatteryIssueDto batteryIssue, string errorMessage)
    {
        await CleanUpDb();
        var batteryResponse = await _httpClient.PostAsJsonAsync("/batteries", new BatteryDto
        { 
            ChargeState = 49M, Voltage = 365.5M
        });

        Assert.Equal(HttpStatusCode.Created, batteryResponse.StatusCode);

        var createdBattery = await batteryResponse.Content.ReadFromJsonAsync<Battery>();
        Assert.NotNull(createdBattery);

        var response = await _httpClient.PostAsJsonAsync($"/batteries/{createdBattery.Id}/issues", batteryIssue);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problemResult = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();

        Assert.NotNull(problemResult?.Errors);
        Assert.Collection(problemResult.Errors, (error) => Assert.Equal(errorMessage, error.Value.First()));
    }
    private async Task CleanUpDb()
    {
        using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetService<BatteryDb>();
            if (db != null && db.Batteries.Any())
            {
                db.Batteries.RemoveRange(db.Batteries);
                await db.SaveChangesAsync();
            }
        }
    }
}