using System.Diagnostics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TaskManagement.Infrastructure;
using TaskManagement.Infrastructure.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.Configure(options =>
    options.ActivityTrackingOptions =
        ActivityTrackingOptions.TraceId |
        ActivityTrackingOptions.SpanId);

builder.Logging.AddSimpleConsole(opt =>
{
    opt.IncludeScopes = true;
    opt.SingleLine = true;
});

builder.Services.AddControllers()
    .AddJsonOptions(opt =>
        opt.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Task Management API", Version = "v1" });
    string[] xmlDocs = ["TaskManagement.Api.xml", "TaskManagement.Application.xml"];
    foreach (var xml in xmlDocs)
    {
        var path = Path.Combine(AppContext.BaseDirectory, xml);
        if (File.Exists(path)) c.IncludeXmlComments(path);
    }
});
builder.Services.AddProblemDetails();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("TaskManagement.Api"))
    .WithTracing(t => t
        .SetSampler(new AlwaysOnSampler())
        .AddAspNetCoreInstrumentation(options =>
        {
            options.EnrichWithHttpResponse = (activity, response) =>
            {
                var route = response.HttpContext.GetRouteData()?.Values;
                if (route?.TryGetValue("controller", out var ctrl) == true)
                    activity.SetTag("aspnet.controller", ctrl?.ToString());
                if (route?.TryGetValue("action", out var action) == true)
                    activity.SetTag("aspnet.action", action?.ToString());
            };
        })
        .AddHttpClientInstrumentation()
        .AddSource("Npgsql")
        .AddSource("TaskManagement.Application")
        .AddSource(RabbitMqPublisher.ActivitySourceName)
        .AddOtlpExporter());

builder.WebHost.UseUrls("http://0.0.0.0:8080");

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Task Management API v1"));
}

app.Use(async (ctx, next) =>
{
    ctx.Response.OnStarting(() =>
    {
        var traceId = Activity.Current?.TraceId.ToString();
        if (traceId is not null)
            ctx.Response.Headers.TryAdd("X-Trace-Id", traceId);
        return Task.CompletedTask;
    });
    await next();
});

app.MapControllers();

app.Run();
