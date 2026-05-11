using CalendarLife.Infrastructure.Persistence;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Linq;

namespace CalendarLife.Api;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CalendarLifeDbContext>
{
    public CalendarLifeDbContext CreateDbContext(string[] args)
    {
        // Load .env when running `dotnet ef` (design-time)
        // `dotnet ef` may set the working directory to the startup project folder.
        // We search a few candidate locations for the solution-root .env.
        var candidates = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), ".env"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", ".env"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".env")
        };

        var envPath = candidates.FirstOrDefault(File.Exists);
        if (envPath is not null)
        {
            // DotNetEnv can populate Environment variables, but we also parse manually
            // to avoid any design-time quirks.
            Env.Load(envPath);

            foreach (var line in File.ReadAllLines(envPath))
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith('#')) continue;
                var idx = trimmed.IndexOf('=');
                if (idx <= 0) continue;

                var key = trimmed[..idx].Trim();
                var value = trimmed[(idx + 1)..].Trim();
                if (key.Length == 0) continue;

                Environment.SetEnvironmentVariable(key, value);
            }
        }

        var cs = Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__DEFAULT");
        if (string.IsNullOrWhiteSpace(cs))
        {
            throw new InvalidOperationException(
                "Missing CONNECTIONSTRINGS__DEFAULT. Create a .env in the solution root or export the env var.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<CalendarLifeDbContext>();
        optionsBuilder.UseNpgsql(cs);

        return new CalendarLifeDbContext(optionsBuilder.Options);
    }
}
