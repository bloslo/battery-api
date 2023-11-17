using BatteryApi.Data;

namespace BatteryApi.Filters;

public class BatteryDtoIsValidFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var param = context.GetArgument<BatteryDto>(0);

        var validationErrors = Utilities.IsValid(param);

        if (validationErrors.Any())
        {
            return Results.ValidationProblem(validationErrors);
        }

        return await next(context);
    }
}
