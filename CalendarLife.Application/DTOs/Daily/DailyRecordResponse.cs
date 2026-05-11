using CalendarLife.Domain.Enums;

namespace CalendarLife.Application.DTOs.Daily;

public sealed record DailyRecordResponse(
    Guid Id,
    Guid UserId,
    int DayIndex,
    string Journal,
    DayStatus DayStatus
);
