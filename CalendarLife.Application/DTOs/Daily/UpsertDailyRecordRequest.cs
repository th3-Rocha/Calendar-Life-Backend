using CalendarLife.Domain.Enums;

namespace CalendarLife.Application.DTOs.Daily;

public sealed record UpsertDailyRecordRequest(
    string Journal,
    DayStatus DayStatus
);
