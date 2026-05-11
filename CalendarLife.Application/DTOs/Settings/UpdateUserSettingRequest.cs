namespace CalendarLife.Application.DTOs.Settings;

public sealed record UpdateUserSettingRequest(
    int SquareSize,
    bool ShowHelp,
    bool SetupCompleted
);
