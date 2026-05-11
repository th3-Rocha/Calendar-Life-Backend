using CalendarLife.Domain.Enums;

namespace CalendarLife.Domain.Entities;


public class DailyTask
{
    public Guid Id { get; set; }
    public Guid DailyRecordId { get; set; }
    public DailyRecord DailyRecord { get; set; } = null!;
    public Guid? SourceDefaultTaskId { get; set; }
    public DefaultTask? SourceDefaultTask { get; set; }
    public string Text { get; set; } = string.Empty;
    public int Duration { get; set; } = 0;
    public DayStatus Status { get; set; } = DayStatus.Pending;
}
