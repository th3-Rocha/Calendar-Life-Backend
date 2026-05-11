using CalendarLife.Application.DTOs.Bootstrap;

using CalendarLife.Application.Interfaces;
using CalendarLife.Domain.Entities;
using CalendarLife.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace CalendarLife.Application.Services;

public sealed class BootstrapService
{
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> UserLocks = new();

    private readonly ICalendarLifeDbContext _db;
    private readonly UserService _users;

    public BootstrapService(ICalendarLifeDbContext db, UserService users)
    {
        _db = db;
        _users = users;
    }

    public async Task<BootstrapStateResponse> GetStateAsync(Guid userId, CancellationToken ct)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId, ct);

        // If user doesn't exist, auto-create them and return the default empty state
        if (user is null)
        {
            var newUserResponse = await _users.GetOrCreateUserAsync(userId, null, ct);
            return new BootstrapStateResponse(
                UserId: newUserResponse.Id,
                Name: newUserResponse.Name,
                BirthDate: newUserResponse.BirthDate,
                SquareSize: 14,
                ShowHelp: false,
                SetupCompleted: false,
                DefaultTasks: Array.Empty<BootstrapStateDefaultTaskDto>(),
                Statuses: new Dictionary<int, DayStatus>(),
                Days: new Dictionary<int, BootstrapStateDayDto>()
            );
        }

        var settings = await _db.UserSettings.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId, ct);

        var defaults = await _db.DefaultTasks
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Text)
            .Select(x => new BootstrapStateDefaultTaskDto(x.Id, x.Text, x.Duration, x.Status))
            .ToListAsync(ct);

        // Load all daily records for this user (simple approach for now)
        var records = await _db.DailyRecords
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.DayIndex)
            .Select(x => new
            {
                x.DayIndex,
                x.DayStatus,
                x.Journal,
                Tasks = x.DailyTasks.Select(t => new BootstrapStateDayTaskDto(
                    t.Id,
                    t.Text,
                    t.Status == DayStatus.Completed,
                    t.Duration,
                    t.Status
                )).ToList()
            })
            .ToListAsync(ct);

        var statuses = records.ToDictionary(x => x.DayIndex, x => x.DayStatus);
        var days = records.ToDictionary(
            x => x.DayIndex,
            x => new BootstrapStateDayDto(x.DayStatus, x.Journal, x.Tasks)
        );

        return new BootstrapStateResponse(
            UserId: user.Id,
            Name: user.Name,
            BirthDate: user.BirthDate,
            SquareSize: settings?.SquareSize ?? 14,
            ShowHelp: settings?.ShowHelp ?? false,
            SetupCompleted: settings?.SetupCompleted ?? false,
            DefaultTasks: defaults,
            Statuses: statuses,
            Days: days
        );
    }

    public async Task<BootstrapResponse> ImportAsync(Guid userId, BootstrapRequest request, CancellationToken ct)
    {
        var userLock = UserLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
        await userLock.WaitAsync(ct);
        try
        {
            // Ensure user exists (no tracked update to avoid optimistic concurrency collisions)
            var normalizedName = string.IsNullOrWhiteSpace(request.Name) ? "User" : request.Name.Trim();
            var normalizedBirthDate = request.BirthDate?.Date ?? DateTime.UtcNow.Date;

            var userExists = await _db.Users.AnyAsync(x => x.Id == userId, ct);
            if (!userExists)
            {
                _db.Users.Add(new User
                {
                    Id = userId,
                    Name = normalizedName,
                    BirthDate = normalizedBirthDate
                });
            }
            else
            {
                await _db.Users
                    .Where(x => x.Id == userId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.Name, normalizedName)
                        .SetProperty(x => x.BirthDate, normalizedBirthDate), ct);
            }

            // Settings: full replace (delete + insert) to avoid tracked-update conflicts
            await _db.UserSettings.Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);
            _db.UserSettings.Add(new UserSetting
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SquareSize = request.SquareSize,
                ShowHelp = request.ShowHelp,
                SetupCompleted = request.SetupCompleted
            });

            // Wipe and Replace DefaultTasks properly to avoid tracking conflicts
            await _db.DefaultTasks.Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);

            var defaultTasks = (request.DefaultTasks ?? Array.Empty<BootstrapDefaultTaskDto>())
                .Where(t => !string.IsNullOrWhiteSpace(t.Text))
                .Select(t => new DefaultTask
                {
                    Id = t.Id ?? Guid.NewGuid(),
                    UserId = userId,
                    Text = t.Text.Trim(),
                    Duration = t.Duration,
                    Status = DayStatus.Pending
                })
                .ToList();

            _db.DefaultTasks.AddRange(defaultTasks);

            // Days / Statuses
            // We import from `Days` if present; otherwise we can create empty records based on `Statuses`.
            int importedDays = 0;
            int importedDayTasks = 0;

            // Work with a mutable dictionary, since request.Days may be read-only
            var daysDict = request.Days is null
                ? new Dictionary<int, BootstrapDayDto>()
                : request.Days.ToDictionary(kv => kv.Key, kv => kv.Value);

            // If only statuses provided, synthesize day entries
            if ((request.Days is null || request.Days.Count == 0) && request.Statuses is not null)
            {
                foreach (var kv in request.Statuses)
                    daysDict[kv.Key] = new BootstrapDayDto(kv.Value, null, null);
            }

            // Full replace of days/tasks to avoid any upsert/tracking conflicts.
            await _db.DailyRecords
                .Where(x => x.UserId == userId)
                .ExecuteDeleteAsync(ct);

            foreach (var (dayIndex, dayDto) in daysDict)
            {
                if (dayIndex < 0) continue;

                // Status priority: explicit dayDto.Status; else fall back to Statuses map
                var status = dayDto.Status;
                if (status is null && request.Statuses is not null && request.Statuses.TryGetValue(dayIndex, out var st))
                    status = st;

                var record = new DailyRecord
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    DayIndex = dayIndex,
                    Journal = dayDto.Journal ?? string.Empty,
                    DayStatus = status ?? DayStatus.Pending
                };

                if (dayDto.Tasks is not null)
                {
                    foreach (var t in dayDto.Tasks)
                    {
                        if (string.IsNullOrWhiteSpace(t.Text)) continue;

                        record.DailyTasks.Add(new DailyTask
                        {
                            Id = Guid.NewGuid(),
                            DailyRecordId = record.Id,
                            Text = t.Text.Trim(),
                            Duration = t.Duration,
                            Status = t.Completed ? DayStatus.Completed : DayStatus.Pending
                        });

                        importedDayTasks++;
                    }
                }

                _db.DailyRecords.Add(record);
                importedDays++;
            }

            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
            {
                var detail = ex.InnerException?.Message ?? ex.Message;
                throw new InvalidOperationException($"Bootstrap save failed: {detail}");
            }

            return new BootstrapResponse(
                UserId: userId,
                ImportedDefaultTasks: defaultTasks.Count,
                ImportedDays: importedDays,
                ImportedDayTasks: importedDayTasks
            );
        }
        finally
        {
            userLock.Release();
        }
    }
}
