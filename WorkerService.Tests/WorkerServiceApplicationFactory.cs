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
using System.Text.Json;
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

    private readonly CreateEntityCommandConsumerOptions _createEntityCommandConsumerOptions = new() { QueueName = "queue.1" };

    private ServiceBusClient _serviceBusClient = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder
            .Configure(_ => { })
            .ConfigureTestServices(services =>
            {
                services.AddSingleton(Options.Create(_createEntityCommandConsumerOptions));
                services.AddSingleton(_serviceBusClient);
                services.AddDbContextFactory<WorkerServiceContext>(o => o.UseSqlServer(_msSqlContainer.GetConnectionString()));
            });
    }

    public async Task ResetDatabase()
    {
        var respawner = await Respawner.CreateAsync(_msSqlContainer.GetConnectionString(), new RespawnerOptions
        {
            TablesToIgnore = [Microsoft.EntityFrameworkCore.Migrations.HistoryRepository.DefaultTableName]
        });
        await respawner.ResetAsync(_msSqlContainer.GetConnectionString());
    }

    public async ValueTask InitializeAsync()
    {
        var serviceBusContainerTask = _serviceBusContainer.StartAsync();
        var msSqlContainerTask =_msSqlContainer.StartAsync();
        await Task.WhenAll(serviceBusContainerTask, msSqlContainerTask);

        _serviceBusClient = new ServiceBusClient(_serviceBusContainer.GetConnectionString());

        using var context = await GetContext();
        await context.Database.MigrateAsync();
    }

    public async Task<WorkerServiceContext> GetContext(CancellationToken cancellationToken = default)
    {
        var dbContextFactory = Services.GetRequiredService<IDbContextFactory<WorkerServiceContext>>();
        var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return context;
    }

    public async Task SendMessageAndWaitForConsumption(CreateEntityCommand command, CancellationToken cancellationToken)
    {
        var messageConsumed = false;
        var createEntityCommandConsumer = Services.GetServices<IHostedService>().OfType<CreateEntityCommandConsumer>().First();
        createEntityCommandConsumer.OnMessageConsumed += () => messageConsumed = true;

        var sender = _serviceBusClient.CreateSender(_createEntityCommandConsumerOptions.QueueName);
        await sender.SendMessageAsync(new ServiceBusMessage(JsonSerializer.Serialize(command)), TestContext.Current.CancellationToken);

        while (!messageConsumed)
        {
            await Task.Delay(1000, TestContext.Current.CancellationToken);
        }
    }

    public override ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        var host = Services.GetRequiredService<IHost>();
        host.StopAsync();
        return base.DisposeAsync();
    }
}
