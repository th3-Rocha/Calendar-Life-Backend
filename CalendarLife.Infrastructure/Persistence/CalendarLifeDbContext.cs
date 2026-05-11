using CalendarLife.Application.Interfaces;
using CalendarLife.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CalendarLife.Infrastructure.Persistence;

public sealed class CalendarLifeDbContext : DbContext, ICalendarLifeDbContext
{
    public CalendarLifeDbContext(DbContextOptions<CalendarLifeDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserSetting> UserSettings => Set<UserSetting>();
    public DbSet<DefaultTask> DefaultTasks => Set<DefaultTask>();
    public DbSet<DailyRecord> DailyRecords => Set<DailyRecord>();
    public DbSet<DailyTask> DailyTasks => Set<DailyTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Users
        modelBuilder.Entity<User>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(100);
            b.Property(x => x.BirthDate).HasColumnType("date");

            b.HasOne(x => x.Setting)
                .WithOne(x => x.User)
                .HasForeignKey<UserSetting>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(x => x.DefaultTasks)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(x => x.DailyRecords)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserSettings
        modelBuilder.Entity<UserSetting>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.UserId).IsUnique();
        });

        // DefaultTasks
        modelBuilder.Entity<DefaultTask>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Text).HasMaxLength(200);
            b.HasIndex(x => x.UserId);
        });

        // DailyRecords
        modelBuilder.Entity<DailyRecord>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Journal).HasMaxLength(10_000);
            b.HasIndex(x => new { x.UserId, x.DayIndex }).IsUnique();
        });

        // DailyTasks
        modelBuilder.Entity<DailyTask>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Text).HasMaxLength(200);

            b.HasOne(x => x.DailyRecord)
                .WithMany(x => x.DailyTasks)
                .HasForeignKey(x => x.DailyRecordId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.SourceDefaultTask)
                .WithMany()
                .HasForeignKey(x => x.SourceDefaultTaskId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasIndex(x => x.DailyRecordId);
        });
    }
}
