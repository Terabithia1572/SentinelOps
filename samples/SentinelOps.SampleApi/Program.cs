using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Service telemetry definitions
const string ServiceName = "OrderProcessingService";
const string ServiceVersion = "1.0.0";
var activitySource = new ActivitySource(ServiceName, ServiceVersion);
var meter = new Meter(ServiceName, ServiceVersion);

var ordersCreatedCounter = meter.CreateCounter<long>("orders.created", description: "Count of processed orders");
var orderValueHistogram = meter.CreateHistogram<double>("orders.value_usd", description: "Value of processed orders in USD");

var ingestionEndpoint = builder.Configuration["Telemetry:Endpoint"] ?? "http://localhost:5100";
var apiKey = builder.Configuration["Telemetry:ApiKey"] ?? "sent_live_default_key";

// Resource configuration
var resourceBuilder = ResourceBuilder.CreateDefault()
    .AddService(ServiceName, serviceVersion: ServiceVersion)
    .AddAttributes(new Dictionary<string, object>
    {
        ["deployment.environment"] = builder.Environment.EnvironmentName,
        ["host.name"] = Environment.MachineName
    });

// Configure OpenTelemetry Tracing & Metrics
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .SetResourceBuilder(resourceBuilder)
            .AddSource(ServiceName)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(opt =>
            {
                opt.Endpoint = new Uri($"{ingestionEndpoint}/v1/traces");
                opt.Protocol = OtlpExportProtocol.HttpProtobuf;
                opt.Headers = $"X-API-Key={apiKey}";
            });
    })
    .WithMetrics(metrics =>
    {
        metrics
            .SetResourceBuilder(resourceBuilder)
            .AddMeter(ServiceName)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(opt =>
            {
                opt.Endpoint = new Uri($"{ingestionEndpoint}/v1/metrics");
                opt.Protocol = OtlpExportProtocol.HttpProtobuf;
                opt.Headers = $"X-API-Key={apiKey}";
            });
    });

// Configure OpenTelemetry Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddOpenTelemetry(logging =>
{
    logging.SetResourceBuilder(resourceBuilder);
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
    logging.AddOtlpExporter(opt =>
    {
        opt.Endpoint = new Uri($"{ingestionEndpoint}/v1/logs");
        opt.Protocol = OtlpExportProtocol.HttpProtobuf;
        opt.Headers = $"X-API-Key={apiKey}";
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => Results.Ok(new
{
    Service = ServiceName,
    Version = ServiceVersion,
    Status = "Healthy",
    Timestamp = DateTime.UtcNow
}));

app.MapGet("/api/orders", (ILogger<Program> logger) =>
{
    logger.LogInformation("Retrieving active orders from catalog");
    var orders = new[]
    {
        new { Id = "ORD-1001", Customer = "Acme Corp", Amount = 1450.00, Status = "Completed" },
        new { Id = "ORD-1002", Customer = "Globex Inc", Amount = 320.50, Status = "Processing" }
    };
    return Results.Ok(orders);
});

app.MapPost("/api/orders", async (OrderRequest order, ILogger<Program> logger) =>
{
    using var activity = activitySource.StartActivity("ProcessOrderTransaction");
    activity?.SetTag("order.id", order.OrderId);
    activity?.SetTag("customer.name", order.Customer);

    logger.LogInformation("Beginning transaction for order {OrderId} amounting to {Amount:C}", order.OrderId, order.Amount);

    // Step 1: Validate
    using (var subActivity = activitySource.StartActivity("ValidateInventory"))
    {
        subActivity?.SetTag("order.item_count", order.ItemCount);
        await Task.Delay(25); // Simulated database lookup
    }

    // Step 2: Payment
    using (var subActivity = activitySource.StartActivity("ExecutePaymentGateway"))
    {
        subActivity?.SetTag("payment.provider", "Stripe");
        await Task.Delay(40); // Simulated payment gateway call
    }

    ordersCreatedCounter.Add(1, new KeyValuePair<string, object?>("status", "success"));
    orderValueHistogram.Record(order.Amount);

    logger.LogInformation("Order {OrderId} processed successfully", order.OrderId);

    return Results.Created($"/api/orders/{order.OrderId}", new
    {
        order.OrderId,
        Status = "Approved",
        ProcessedAt = DateTime.UtcNow
    });
});

app.MapGet("/api/orders/{id}/slow", async (string id, ILogger<Program> logger) =>
{
    using var activity = activitySource.StartActivity("SlowDatabaseQuery");
    activity?.SetTag("query.table", "OrderHistory");

    logger.LogWarning("Execution threshold exceeded: Running heavy historical aggregation query for order {OrderId}", id);
    await Task.Delay(800); // 800ms latency simulation

    return Results.Ok(new { OrderId = id, ExecutionTimeMs = 800, Status = "Completed" });
});

app.MapGet("/api/orders/simulate-error", (ILogger<Program> logger) =>
{
    using var activity = activitySource.StartActivity("FailingExternalCall");
    activity?.SetStatus(ActivityStatusCode.Error, "Downstream payment gateway timeout");

    try
    {
        throw new TimeoutException("Connection to payment gateway timed out after 5000ms at Gateway.ProcessAsync");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Critical failure during payment processing: {ErrorMessage}", ex.Message);
        return Results.Problem(
            detail: ex.Message,
            statusCode: StatusCodes.Status500InternalServerError,
            title: "Simulated Downstream Failure");
    }
});

app.Run();

public record OrderRequest(string OrderId, string Customer, double Amount, int ItemCount);
