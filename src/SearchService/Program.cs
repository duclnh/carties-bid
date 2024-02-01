using System.Net;
using MassTransit;
using Polly;
using Polly.Extensions.Http;
using SearchService;
using SearchService.Consumers;
using SearchService.Data;
using SearchService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddHttpClient<AuctionSvcHttpClient>().AddPolicyHandler(GetPolicy());
builder.Services.AddMassTransit(x =>
{
    x.AddConsumersFromNamespaceContaining<AuctionCreatedConsumer>();
    x.AddConsumersFromNamespaceContaining<AuctionUpdateConsumer>();
    x.AddConsumersFromNamespaceContaining<AuctionDeletedConsumer>();
    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("search", false));
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"], "/", h =>
      {
          h.Username(builder.Configuration.GetValue("RabbitMQ:Username", "guest"));
          h.Password(builder.Configuration.GetValue("RabbitMQ:Password", "guest"));
      });
        cfg.ReceiveEndpoint("search-auction-created-", e =>
        {
            e.UseMessageRetry(r => r.Interval(5, 5));// 5 time , 1time is 5s
            e.ConfigureConsumer<AuctionCreatedConsumer>(context);
        });
        cfg.ConfigureEndpoints(context);
    });
});
var app = builder.Build();


app.UseAuthorization();

app.MapControllers();
app.Lifetime.ApplicationStarted.Register(async () =>
{
    try
    {
        await DbInitializer.InitDb(app);
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error registering: " + ex.Message);
    }
});
app.Run();
static IAsyncPolicy<HttpResponseMessage> GetPolicy()
    => HttpPolicyExtensions
       .HandleTransientHttpError()
       .OrResult(msg => msg.StatusCode == HttpStatusCode.NotFound)
       .WaitAndRetryForeverAsync(_ => TimeSpan.FromSeconds(3));