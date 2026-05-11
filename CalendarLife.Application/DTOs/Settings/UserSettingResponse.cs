namespace CalendarLife.Application.DTOs.Settings;

public sealed record UserSettingResponse(
    Guid UserId,
    int SquareSize,
    bool ShowHelp,
    bool SetupCompleted
);
