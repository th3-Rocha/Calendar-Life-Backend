namespace CalendarLife.Application.DTOs.Users;

public sealed record UpsertUserRequest(
    string? Name,
    DateTime? BirthDate
);
