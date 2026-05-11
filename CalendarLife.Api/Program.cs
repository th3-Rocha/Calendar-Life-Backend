using CalendarLife.Application.Interfaces;
using CalendarLife.Application.Services;
using CalendarLife.Infrastructure.Persistence;
using DotNetEnv;
using System.IO;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Load .env into process environment variables (local/dev convenience)
// In runtime, the working directory may be CalendarLife.Api, while the repo root contains .env.
// We try both the API content root and the repo root (parent folder).
Env.Load(Path.Combine(builder.Environment.ContentRootPath, ".env"));
Env.Load(Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", ".env")));

// Controllers + Swagger
builder.Services.AddControllers().AddJsonOptions(options =>
{
    // Allow enums like "completed" / "failed" / "pending" (case-insensitive)
    // instead of requiring "Completed" / "Failed" / "Pending".
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});
builder.Services.AddOpenApi();
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ProblemDetails for consistent error responses
builder.Services.AddProblemDetails();

// Database
builder.Services.AddDbContext<CalendarLifeDbContext>(options =>
{
    var cs = builder.Configuration.GetConnectionString("Default");
    if (string.IsNullOrWhiteSpace(cs))
    {
        // DotNetEnv loads variables like CONNECTIONSTRINGS__DEFAULT (double underscore maps to "ConnectionStrings:Default")
        // but configuration binding can vary depending on how the host is launched.
        cs = Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__DEFAULT");
    }

    if (string.IsNullOrWhiteSpace(cs))
        throw new InvalidOperationException("Missing connection string 'ConnectionStrings:Default'. Set CONNECTIONSTRINGS__DEFAULT in your .env.");

    options.UseNpgsql(cs);
});

builder.Services.AddScoped<ICalendarLifeDbContext>(sp => sp.GetRequiredService<CalendarLifeDbContext>());

// Application services
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<BootstrapService>();

// Basic throttling (rate limiting)
// NOTE: This is keyed by client IP for now. Later we can key by userId per endpoint.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter(policyName: "default", limiterOptions =>
    {
        limiterOptions.PermitLimit = 60; // 60 requests
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Global exception handler -> ProblemDetails JSON
app.UseExceptionHandler(exceptionApp =>
{
    exceptionApp.Run(async context =>
    {
        var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var ex = feature?.Error;

        var (status, title) = ex switch
        {
            // Your app/business validation errors
            ArgumentException => (StatusCodes.Status400BadRequest, "Bad request"),
            InvalidOperationException => (StatusCodes.Status400BadRequest, "Invalid operation"),

            // Database/update errors (usually bad input or constraint violations)
            Microsoft.EntityFrameworkCore.DbUpdateException => (StatusCodes.Status409Conflict, "Database update failed"),

            _ => (StatusCodes.Status500InternalServerError, "Server error")
        };

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = app.Environment.IsDevelopment() ? ex?.Message : null,
            Instance = context.Request.Path
        };

        problem.Extensions["traceId"] = context.TraceIdentifier;
        if (app.Environment.IsDevelopment() && ex is not null)
        {
            problem.Extensions["exceptionType"] = ex.GetType().FullName;
        }

        await context.Response.WriteAsJsonAsync(problem);
    });
});

// Heroku sits behind a reverse proxy. This makes ASP.NET Core respect X-Forwarded-* headers
// so Request.Scheme becomes "https" when the original request was HTTPS.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor
});

app.UseCors("AllowAll");

// Heroku router already terminates TLS and enforces HTTPS for your app.
// Keeping HTTPS redirection here can be noisy/misconfigured unless you fully configure ports.
// app.UseHttpsRedirection();

app.UseRateLimiter();
app.UseCors(MyAllowSpecificOrigins);
app.MapControllers();

app.Run();
