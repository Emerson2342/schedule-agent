using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
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
builder.Services.AddSingleton(kernelBuilder.Build());
builder.Services.AddSingleton<Dictionary<string, ChatHistory>>();


//builder.Services.AddSingleton<PermissionService>();




builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFront", policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .WithOrigins("http://192.168.0.105:3000");
    });
});

var app = builder.Build();


app.UseCors("AllowFront");

app.MapControllers();
app.Run();
public record PromptRequest(string Prompt);


