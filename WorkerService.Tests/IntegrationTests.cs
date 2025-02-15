using Azure.Messaging.ServiceBus;
using FluentAssertions;
using System.Text.Json;
using WorkerService.Persistence.Entities;

namespace WorkerService;

public class IntegrationTests(WorkerServiceApplicationFactory workerServiceApplicationFactory) : IClassFixture<WorkerServiceApplicationFactory>
{
    private readonly WorkerServiceApplicationFactory _workerServiceApplicationFactory = workerServiceApplicationFactory;

    [Fact]
    public async Task CanCreateTestEntity()
    {
        // Arrange
        using var host = await _workerServiceApplicationFactory.RunHostAsync(TestContext.Current.CancellationToken);
        var sender = _workerServiceApplicationFactory.ServiceBusClient.CreateSender("queue.1");
        bool messageConsumed = false;
        var worker = _workerServiceApplicationFactory.Worker.OnMessageConsumed += () => messageConsumed = true;

        var command = new CreateEntityCommand(Guid.Parse("{82672914-A1B6-442B-943C-0A6F2A594DD8}"), "Test");

        var expectedEntity = new TestEntity(command.Id, command.Name);

        // Act
        await sender.SendMessageAsync(new ServiceBusMessage(JsonSerializer.Serialize(command)), TestContext.Current.CancellationToken);

        while (messageConsumed is false)
        {
            await Task.Delay(1000, TestContext.Current.CancellationToken);
        }

        // Assert
        using var context = await _workerServiceApplicationFactory.GetContext(TestContext.Current.CancellationToken);
        var createdEntity = context.TestEntities.First();
        createdEntity.Should().BeEquivalentTo(expectedEntity);
    }
}
