
using BatteryApi.Models;

namespace BatteryApi.Filters;

public class BatteryIssueDtoIsValidFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var param = context.GetArgument<BatteryIssueDto>(1);

        var validationErrors = Utilities.IsValid(param);

        if (validationErrors.Any())
        {
            return Results.ValidationProblem(validationErrors);
        }

        return await next(context);
    }
}
