namespace CalendarLife.Application.DTOs.Tasks;

public sealed record TaskItemDto(
    Guid? Id,
    string Text,
    int Duration
);
