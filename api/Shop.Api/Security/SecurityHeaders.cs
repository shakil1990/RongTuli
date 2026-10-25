namespace Shop.Api.Security;

/// <summary>
/// Response headers for an API that only ever answers JSON: nothing it returns should be framed,
/// sniffed into another type, or allowed to load anything.
/// </summary>
public static class SecurityHeaders
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, IHostEnvironment environment)
    {
        // Added as the response starts, because the exception handler clears any headers set
        // before it runs and a 500 deserves them as much as a 200.
        return app.Use((context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers.XContentTypeOptions = "nosniff";
                headers.XFrameOptions = "DENY";
                headers.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";
                headers["Referrer-Policy"] = "no-referrer";

                // Only where the host serves HTTPS; locally it would pin localhost to TLS.
                if (!environment.IsDevelopment())
                {
                    headers.StrictTransportSecurity = "max-age=31536000";
                }

                return Task.CompletedTask;
            });

            return next(context);
        });
    }
}
