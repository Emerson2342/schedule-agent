using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using ScheduleAgent.data;
using ScheduleAgent.Plugins;


var builder = WebApplication.CreateBuilder(args);

var kernelBuilder = Kernel.CreateBuilder()
    .AddOllamaChatCompletion(
        modelId: "llama3.1:latest",
        endpoint: new Uri("http://localhost:11434")
    );

builder.Services.AddDbContext<AppDbContext>(opt =>
{
    opt.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(9, 0, 0))
    );
});

builder.Services.AddControllers();
builder.Services.AddScoped<SchedulePlugin>();
builder.Services.AddScoped(sp =>
{
    var kernel = kernelBuilder.Build();

    var plugin = sp.GetRequiredService<SchedulePlugin>();
    kernel.Plugins.AddFromObject(plugin, nameof(SchedulePlugin));

    return kernel;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFront", policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .SetIsOriginAllowed(origin => true)
            .AllowCredentials();
    });
});

var app = builder.Build();


app.UseCors("AllowFront");

app.MapControllers();
app.Run();
public record PromptRequest(string Prompt);


