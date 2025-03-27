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
    public static RouteHandlerBuilder RequireAuthentication(this RouteHandlerBuilder builder)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var HttpContext = context.HttpContext;
            var userRole = HttpContext.Session.GetString("role");

            if (userRole == null)
            {
                return Results.Unauthorized();
            }

            return await next(context);
        });
    }

    public static RouteHandlerBuilder RequireAuthenticationWithId(this RouteHandlerBuilder builder)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var HttpContext = context.HttpContext;
            var userId = HttpContext.Session.GetInt32("id");
            var userRole = HttpContext.Session.GetString("role");

            if (userRole == "admin")
            {
                return await next(context);
            }

            if (userId == null)
            {
                return Results.Unauthorized();
            }

            if (context.Arguments[0] is int routeId && routeId != userId)
            {
                return Results.StatusCode(403);
            }

            return await next(context);

        });
    }
}