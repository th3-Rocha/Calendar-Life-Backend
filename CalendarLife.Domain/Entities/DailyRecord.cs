using CalendarLife.Domain.Enums;

namespace CalendarLife.Domain.Entities;


public class DailyRecord
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public int DayIndex { get; set; } //0 a 200000
    public string Journal { get; set; } = string.Empty;
    public DayStatus DayStatus { get; set; } = DayStatus.Pending;
    public ICollection<DailyTask> DailyTasks { get; set; } = new List<DailyTask>();
}
