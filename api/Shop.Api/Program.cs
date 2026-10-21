using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Shop.Api;
using Shop.Api.Cart;
using Shop.Api.Catalog;
using Shop.Api.Images;
using Shop.Api.Security;

const string CorsPolicy = "Frontend";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddShopData(builder.Configuration);
builder.Services.AddAdminAuthorization();
builder.Services.AddImageStore(builder.Configuration);
builder.Services.AddCors(options =>
    options.AddPolicy(CorsPolicy, policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [])
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors(CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapCatalogEndpoints();
app.MapAdminCatalogEndpoints();
app.MapProductImageEndpoints();
app.MapCartEndpoints();

// Reports only healthy/unhealthy so the public endpoint never leaks connection details.
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = (context, report) => context.Response.WriteAsJsonAsync(new
    {
        status = report.Status == HealthStatus.Healthy ? "healthy" : "unhealthy"
    })
});

app.Run();

public partial class Program;
