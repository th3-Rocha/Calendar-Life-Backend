namespace CalendarLife.Application.DTOs.Users;

public sealed record UserResponse(
    Guid Id,
    string Name,
    DateTime BirthDate
);
