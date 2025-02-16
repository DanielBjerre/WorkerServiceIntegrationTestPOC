using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WorkerService.Persistence;
using WorkerService.Persistence.Entities;

namespace WorkerService;

public class CreateEntityCommandConsumer(
    ILogger<CreateEntityCommandConsumer> logger,
    ServiceBusClient serviceBusClient,
    IOptions<CreateEntityCommandConsumerOptions> options,
    IDbContextFactory<WorkerServiceContext> workerServiceContextFactory) : MessageConsumer<CreateEntityCommand>(logger, serviceBusClient)
{
    private readonly IOptions<CreateEntityCommandConsumerOptions> options = options;
    private readonly IDbContextFactory<WorkerServiceContext> _workerServiceContextFactory = workerServiceContextFactory;

    protected override string QueueName => options.Value.QueueName;

    protected override async Task HandleMessage(CreateEntityCommand messageBody, ServiceBusReceivedMessage message, CancellationToken cancellationToken)
    {
        using var context = await _workerServiceContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = new TestEntity(messageBody.Id, messageBody.Name);
        context.TestEntities.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
    }
}
public record CreateEntityCommand(Guid Id, string Name);

public class CreateEntityCommandConsumerOptions
{
    public const string Section = "CreateEntityCommandConsumer";
    public required string QueueName { get; set; }
}
