using Azure.Messaging.ServiceBus;
using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Respawn;
using Testcontainers.MsSql;
using Testcontainers.ServiceBus;
using WorkerService.Persistence;

namespace WorkerService;

public class WorkerServiceApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly ServiceBusContainer _serviceBusContainer = new ServiceBusBuilder()
        .WithImage("mcr.microsoft.com/azure-messaging/servicebus-emulator:latest")
        .WithAcceptLicenseAgreement(true)
        .Build();

    private readonly MsSqlContainer _msSqlContainer = new MsSqlBuilder()
        .WithPortBinding(1433, true)
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1433))
        .Build();

    public CreateEntityCommandConsumer Worker { get; private set; } = null!;
    public ServiceBusClient ServiceBusClient { get; private set; } = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var options = Options.Create(new CreateEntityCommandConsumerOptions { QueueName = "queue.1" });

        builder
            .Configure(_ => { })
            .ConfigureTestServices(services =>
            {
                services.AddSingleton(options);
                services.AddSingleton(ServiceBusClient);
                services.AddDbContextFactory<WorkerServiceContext>(o => o.UseSqlServer(_msSqlContainer.GetConnectionString()));
            });
    }

    public async Task<IHost> RunHostAsync(CancellationToken cancellationToken)
    {
        var respawner = await Respawner.CreateAsync(_msSqlContainer.GetConnectionString(), new RespawnerOptions
        {
            TablesToIgnore = [Microsoft.EntityFrameworkCore.Migrations.HistoryRepository.DefaultTableName]
        });
        await respawner.ResetAsync(_msSqlContainer.GetConnectionString());

        var host = Services.GetRequiredService<IHost>();
        Worker = Services.GetServices<IHostedService>().OfType<CreateEntityCommandConsumer>().First();
        await host.StartAsync(cancellationToken);
        return host;
    }

    public async ValueTask InitializeAsync()
    {
        var serviceBusContainerTask = _serviceBusContainer.StartAsync();
        var msSqlContainerTask =_msSqlContainer.StartAsync();
        await Task.WhenAll(serviceBusContainerTask, msSqlContainerTask);
        ServiceBusClient = new ServiceBusClient(_serviceBusContainer.GetConnectionString());

        using var context = await GetContext();
        await context.Database.MigrateAsync();

    }

    public async Task<WorkerServiceContext> GetContext(CancellationToken cancellationToken = default)
    {
        var dbContextFactory = Services.GetRequiredService<IDbContextFactory<WorkerServiceContext>>();
        var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return context;
    }
}
