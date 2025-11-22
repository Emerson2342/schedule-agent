namespace ScheduleAgent.data;

using Microsoft.EntityFrameworkCore;
using ScheduleAgent.Models;

public class AppDbContext : DbContext
{
    public DbSet<Schedule> Schedules => Set<Schedule>();

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }
}
