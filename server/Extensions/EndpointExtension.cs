namespace server.Extensions;

public static class EndpointExtensions
{
    public static RouteHandlerBuilder RequireRole(this RouteHandlerBuilder builder, string role)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var HttpContext = context.HttpContext;
            var userRole = HttpContext.Session.GetString("role");

            if (userRole == null)
            {
                return Results.Unauthorized();
            }

            if (userRole != role)
            {
                return Results.StatusCode(403);
            }

            return await next(context);
        });
    }
}