using MassTransit;
using NotificationService.Consumers;
using NotificationService.Hubs;

var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
builder.Services.AddMassTransit(x =>
{
    x.AddConsumersFromNamespaceContaining<AuctionCreatedConsumer>();
    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("nt", false));

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.UseMessageRetry(r =>
        {
            r.Handle<RabbitMqConnectionException>();
            r.Interval(5, TimeSpan.FromSeconds(10));
        });

        cfg.Host(builder.Configuration["RabbitMQ:Host"], "/", h =>
        {
            h.Username(builder.Configuration.GetValue("RabbitMQ:Username", "guest"));
            h.Password(builder.Configuration.GetValue("RabbitMQ:Password", "guest"));
        });
        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddSignalR();
var app = builder.Build();
app.MapHub<NotificationHub>("/notifications");
app.Run();
