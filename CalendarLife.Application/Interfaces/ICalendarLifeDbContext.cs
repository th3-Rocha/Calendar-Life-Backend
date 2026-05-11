using CalendarLife.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CalendarLife.Application.Interfaces;

public interface ICalendarLifeDbContext
{
    DbSet<User> Users { get; }
    DbSet<UserSetting> UserSettings { get; }
    DbSet<DefaultTask> DefaultTasks { get; }
    DbSet<DailyRecord> DailyRecords { get; }
    DbSet<DailyTask> DailyTasks { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
