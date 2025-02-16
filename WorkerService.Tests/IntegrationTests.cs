using FluentAssertions;
using WorkerService.Persistence.Entities;

namespace WorkerService;

public class IntegrationTests(WorkerServiceApplicationFactory workerServiceApplicationFactory) : IClassFixture<WorkerServiceApplicationFactory>
{
    private readonly WorkerServiceApplicationFactory _workerServiceApplicationFactory = workerServiceApplicationFactory;

    [Fact]
    public async Task CanCreateTestEntity()
    {
        // Arrange
        var command = new CreateEntityCommand(Guid.Parse("{82672914-A1B6-442B-943C-0A6F2A594DD8}"), "Test");
        var expectedEntity = new TestEntity(command.Id, command.Name);

        // Act
        await _workerServiceApplicationFactory.SendMessageAndWaitForConsumption(command, TestContext.Current.CancellationToken);

        // Assert
        using var context = await _workerServiceApplicationFactory.GetContext(TestContext.Current.CancellationToken);
        var createdEntity = await context.TestEntities.FindAsync([command.Id], TestContext.Current.CancellationToken);
        createdEntity.Should().BeEquivalentTo(expectedEntity);
    }
}
