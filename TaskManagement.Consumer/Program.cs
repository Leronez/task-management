using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TaskManagement.Consumer.Workers;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.Configure(opt =>
            opt.ActivityTrackingOptions =
                ActivityTrackingOptions.TraceId |
                ActivityTrackingOptions.SpanId);
        logging.AddSimpleConsole(opt =>
        {
            opt.IncludeScopes = true;
            opt.SingleLine = true;
        });
    })
    .ConfigureServices(services =>
    {
        services.AddHostedService<TaskEventConsumer>();
        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService("TaskManagement.Consumer"))
            .WithTracing(t => t
                .SetSampler(new AlwaysOnSampler())
                .AddSource(TaskEventConsumer.ActivitySourceName)
                .AddOtlpExporter());
    })
    .Build();

await host.RunAsync();
