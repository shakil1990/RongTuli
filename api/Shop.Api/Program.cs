using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Shop.Api;
using Shop.Api.Auth;
using Shop.Api.Cart;
using Shop.Api.Catalog;
using Shop.Api.Images;
using Shop.Api.Orders;
using Shop.Api.Phone;
using Shop.Api.Security;

const string CorsPolicy = "Frontend";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddShopData(builder.Configuration);
builder.Services.AddAdminAuthorization(builder.Configuration);
builder.Services.AddImageStore(builder.Configuration);
builder.Services.AddScoped<GoogleSignIn>();
builder.Services.AddPhoneVerification(builder.Configuration, builder.Environment);
builder.Services.Configure<OrderOptions>(builder.Configuration.GetSection(OrderOptions.SectionName));
builder.Services.AddScoped<OrderPlacement>();
builder.Services.AddCors(options =>
    options.AddPolicy(CorsPolicy, policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [])
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

// First, so everything after it — the per-IP rate limits above all — sees the caller's address.
app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors(CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapAuthEndpoints();
app.MapCatalogEndpoints();
app.MapAdminCatalogEndpoints();
app.MapProductImageEndpoints();
app.MapCartEndpoints();
app.MapPhoneEndpoints();
app.MapOrderEndpoints();
app.MapAdminOrderEndpoints();

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
