namespace ScheduleAgent.Models;

public class Schedule
{
    public int Id { get; set; }

    public DateTime Date { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
}
