namespace CalendarLife.Application.DTOs.Bootstrap;

public sealed record BootstrapResponse(
    Guid UserId,
    int ImportedDefaultTasks,
    int ImportedDays,
    int ImportedDayTasks
);
