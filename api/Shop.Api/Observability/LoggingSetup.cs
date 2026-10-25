using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace Shop.Api.Observability;

/// <summary>
/// Serilog behind <see cref="ILogger{TCategoryName}"/>: code keeps logging through the abstraction,
/// and only this file knows which library writes the lines. Levels come from the "Serilog"
/// section of configuration; the sink is always the console, which is what the host collects.
/// </summary>
public static class LoggingSetup
{
    private const string DevelopmentTemplate =
        "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";

    public static WebApplicationBuilder AddShopLogging(this WebApplicationBuilder builder)
    {
        // The static Log.Logger is left alone: nothing here uses it, and test hosts would race to set it.
        builder.Services.AddSerilog(
            (services, logger) =>
            {
                logger.ReadFrom.Configuration(builder.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext();

                // One JSON object per line outside Development: every value is escaped, so nothing
                // a caller sends can forge a log line, and each event carries its trace id — the
                // same id a ProblemDetails response shows as traceId.
                if (builder.Environment.IsDevelopment())
                {
                    logger.WriteTo.Console(outputTemplate: DevelopmentTemplate);
                }
                else
                {
                    logger.WriteTo.Console(new RenderedCompactJsonFormatter());
                }
            },
            preserveStaticLogger: true);

        return builder;
    }

    /// <summary>One line per request with its status and duration, in place of the framework's several.</summary>
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app) =>
        app.UseSerilogRequestLogging(options => options.GetLevel = (http, _, exception) =>
            exception is not null || http.Response.StatusCode >= StatusCodes.Status500InternalServerError
                ? LogEventLevel.Error
                // The host's health check calls every few seconds; only a failing one is worth a line.
                : http.Request.Path == "/health" && http.Response.StatusCode == StatusCodes.Status200OK
                    ? LogEventLevel.Verbose
                    : LogEventLevel.Information);
}
