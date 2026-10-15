using Serilog;
using SentinelOps.Application;
using SentinelOps.Infrastructure;
using SentinelOps.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

// Serilog configuration
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Services.AddSerilog();

// Layer Dependency Injection
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Background Services
builder.Services.AddHostedService<RetentionCleanupService>();
builder.Services.AddHostedService<AlertEvaluationBackgroundService>();
builder.Services.AddHostedService<NotificationDispatcherBackgroundService>();
builder.Services.AddHostedService<SloEvaluationBackgroundService>();

var host = builder.Build();
host.Run();
