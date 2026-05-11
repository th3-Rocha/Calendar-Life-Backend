namespace CalendarLife.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }


    public UserSetting? Setting { get; set; }
    public ICollection<DefaultTask> DefaultTasks { get; set; } = new List<DefaultTask>();
    public ICollection<DailyRecord> DailyRecords { get; set; } = new List<DailyRecord>();
}
