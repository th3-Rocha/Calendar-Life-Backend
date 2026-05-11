
using CalendarLife.Domain.Enums;

namespace CalendarLife.Domain.Entities;


public class DefaultTask
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string Text { get; set; } = string.Empty;
    public int Duration { get; set; } = 0;
    public DayStatus Status { get; set; } = DayStatus.Pending;
}
