using WorkerService;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<CreateEntityCommandConsumer>();

var host = builder.Build();
host.Run();

public partial class Program { }