using CalendarLife.Domain.Enums;

namespace CalendarLife.Application.DTOs.Bootstrap;

public sealed record BootstrapRequest(
    string? Name,
    DateTime? BirthDate,
    int SquareSize,
    bool ShowHelp,
    bool SetupCompleted,
    IReadOnlyList<BootstrapDefaultTaskDto>? DefaultTasks,
    IReadOnlyDictionary<int, DayStatus>? Statuses,
    IReadOnlyDictionary<int, BootstrapDayDto>? Days
);

public sealed record BootstrapDefaultTaskDto(
    Guid? Id,
    string Text,
    int Duration
);

public sealed record BootstrapDayDto(
    DayStatus? Status,
    string? Journal,
    IReadOnlyList<BootstrapDayTaskDto>? Tasks
);

public sealed record BootstrapDayTaskDto(
    Guid? Id,
    string Text,
    bool Completed,
    int Duration
);
