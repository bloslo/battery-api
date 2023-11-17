using BatteryApi.Data;
using BatteryApi.Filters;
using BatteryApi.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddKeyPerFile(directoryPath: "/run/secrets", optional: true);
var connection = builder.Configuration.GetValue<string>("db_connection");

builder.Services.AddNpgsql<BatteryDb>(connection);
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("healthz");

app.MapGet("/batteries", async (BatteryDb db) =>
    await db.Batteries.ToListAsync());

app.MapGet("/batteries/{id}", async (int id, BatteryDb db) =>
    await db.Batteries.FindAsync(id)
        is Battery battery
            ? Results.Ok(battery)
            : Results.NotFound());

app.MapPost("/batteries", CreateBattery)
    .AddEndpointFilter<BatteryDtoIsValidFilter>();

app.MapPut("/batteries/{id}", UpdateBattery)
    .AddEndpointFilter<BatteryDtoIsValidFilter>();

app.MapDelete("/batteries/{id}", RemoveBattery);

app.MapGet("/batteries/{id}/issues", async (int id, BatteryDb db) =>
{
    var battery = await db.Batteries.Include(battery => battery.BatteryIssues)
        .FirstOrDefaultAsync(battery => battery.Id == id);

    if (battery != null)
    {
        return Results.Ok(battery.BatteryIssues.ToList());
    }

    return Results.NotFound();
});
        // is Battery battery
        //     ? TypedResults.Ok(battery.BatteryIssues)
        //     : TypedResults.NotFound());

app.MapPost("/batteries/{id}/issues", CreateBatteryIssue)
    .AddEndpointFilter<BatteryIssueDtoIsValidFilter>();

app.MapPut("/batteries/{id}/issues/{issueId}", UpdateBatteryIssue)
    .AddEndpointFilter<BatteryIssueDtoIsValidFilter>();

app.MapDelete("/batteries/{id}/issues/{issueId}", RemoveBatteryIssue);

app.Run();

async Task<Created<Battery>> CreateBattery(BatteryDto battery, BatteryDb db)
{
    var newBattery = new Battery
    {
        ChargeState = battery.ChargeState,
        Voltage = battery.Voltage,
        Health = BatteryHealth.Excellent
    };

    if (battery.ChargeState < 20M || battery.ChargeState > 80M)
    {
        newBattery.DailyHealthCounter += 1;
    }

    await db.Batteries.AddAsync(newBattery);
    await db.SaveChangesAsync();

    return TypedResults.Created($"/batteries/{newBattery.Id}", newBattery);
}

async Task<Results<Created<Battery>, NotFound>> UpdateBattery(BatteryDto battery, int id, BatteryDb db)
{
    var existingBattery = await db.Batteries.FindAsync(id);

    if (existingBattery != null)
    {
        existingBattery.ChargeState = battery.ChargeState;
        existingBattery.Voltage = battery.Voltage;

        if (existingBattery.ChargeState < 20M || existingBattery.ChargeState > 80M)
        {
            if (existingBattery.Updated.Date != DateTime.Today)
            {
                existingBattery.DailyHealthCounter = 1;
            }
            else
            {
                existingBattery.DailyHealthCounter += 1;
            }

            if (existingBattery.DailyHealthCounter > 2 && existingBattery.Health != BatteryHealth.Bad)
            {
                existingBattery.Health -= 1;
            }
        }

        await db.SaveChangesAsync();

        return TypedResults.Created($"/batteries/{existingBattery.Id}", existingBattery);
    }

    return TypedResults.NotFound();
}

async Task<Results<NoContent, NotFound>> RemoveBattery(int id, BatteryDb db)
{
    var battery = await db.Batteries.FindAsync(id);

    if (battery != null)
    {
        db.Batteries.Remove(battery);
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    return TypedResults.NotFound();
}

async Task<Created<BatteryIssue>> CreateBatteryIssue(int id, BatteryIssueDto batteryIssue, BatteryDb db)
{
    var newBatteryIssue = new BatteryIssue
    {
        IssueType = batteryIssue.IssueType,
        Description = batteryIssue.Description,
        Occurred = batteryIssue.Occurred,
        BatteryId = id
    };

    await db.BatteryIssues.AddAsync(newBatteryIssue);
    await db.SaveChangesAsync();

    return TypedResults.Created($"/batteries/{newBatteryIssue.BatteryId}/issues", newBatteryIssue);
}

async Task<Results<Created<BatteryIssue>, NotFound>> UpdateBatteryIssue(int id, BatteryIssueDto batteryIssue, int issueId, BatteryDb db)
{
    var existingBatteryIssue = await db.BatteryIssues.FindAsync(issueId);

    if (existingBatteryIssue != null && existingBatteryIssue.BatteryId == id)
    {
        existingBatteryIssue.Description = batteryIssue.Description;
        existingBatteryIssue.IssueType = batteryIssue.IssueType;
        existingBatteryIssue.Occurred = batteryIssue.Occurred;

        await db.SaveChangesAsync();

        return TypedResults.Created($"/batteries/{existingBatteryIssue.BatteryId}/issues", existingBatteryIssue);
    }

    return TypedResults.NotFound();
}

async Task<Results<NoContent, NotFound>> RemoveBatteryIssue(int id, int issueId, BatteryDb db)
{
    var issue = await db.BatteryIssues.FindAsync(issueId);

    if (issue != null && issue.BatteryId == id)
    {
        db.BatteryIssues.Remove(issue);
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    return TypedResults.NotFound();
}

public partial class Program
{ }
