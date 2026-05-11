namespace CalendarLife.Domain.Entities;

public class UserSetting
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public int SquareSize { get; set; } = 14;
    public bool ShowHelp { get; set; } = false;
    public bool SetupCompleted { get; set; } = true;


    public User? User { get; set; }
}
