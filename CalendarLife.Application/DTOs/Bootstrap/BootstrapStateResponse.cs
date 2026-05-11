using CalendarLife.Domain.Enums;

namespace CalendarLife.Application.DTOs.Bootstrap;

public sealed record BootstrapStateResponse(
    Guid UserId,
    string Name,
    DateTime BirthDate,
    int SquareSize,
    bool ShowHelp,
    bool SetupCompleted,
    IReadOnlyList<BootstrapStateDefaultTaskDto> DefaultTasks,
    IReadOnlyDictionary<int, DayStatus> Statuses,
    IReadOnlyDictionary<int, BootstrapStateDayDto> Days
);

public sealed record BootstrapStateDefaultTaskDto(
    Guid Id,
    string Text,
    int Duration,
    DayStatus Status
);

public sealed record BootstrapStateDayDto(
    DayStatus Status,
    string Journal,
    IReadOnlyList<BootstrapStateDayTaskDto> Tasks
);

public sealed record BootstrapStateDayTaskDto(
    Guid Id,
    string Text,
    bool Completed,
    int Duration,
    DayStatus Status
);
