using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using ScheduleAgent.data;
using ScheduleAgent.Models;

namespace ScheduleAgent.Plugins;

public class SchedulePlugin
{
    private readonly AppDbContext _db;

    public SchedulePlugin(AppDbContext db)
    {
        _db = db;
    }

    [KernelFunction("list_event")]
    [Description("Returns all events items")]
    public async Task<List<Schedule>> ListSchedules()
    {
        return await _db.Schedules.ToListAsync();
    }

    [KernelFunction("create_event")]
    [Description("Creates a new event item")]
    public async Task<Schedule> CreateSchedule(DateTime date, string title, string description)
    {
        var item = new Schedule
        {
            Date = date,
            Title = title,
            Description = description
        };

        _db.Schedules.Add(item);
        await _db.SaveChangesAsync();

        return item;
    }
    [KernelFunction("update_event")]
    [Description("Updates an existing event item")]
    public async Task<string> UpdateSchedule(int id, DateTime date, string title, string description)
    {
        var item = await _db.Schedules.FindAsync(id);

        if (item == null)
            return "Event not found.";

        item.Date = date;
        item.Title = title;
        item.Description = description;

        await _db.SaveChangesAsync();
        return "Event updated successfully.";
    }
    [KernelFunction("delete_event")]
    [Description("Deletes a event item")]
    public async Task<string> DeleteSchedule(int id)
    {
        var item = await _db.Schedules.FindAsync(id);

        if (item == null)
            return "Event not found.";

        _db.Schedules.Remove(item);
        await _db.SaveChangesAsync();

        return "Event deleted.";
    }
}