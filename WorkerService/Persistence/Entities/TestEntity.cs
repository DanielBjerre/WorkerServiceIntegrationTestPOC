namespace WorkerService.Persistence.Entities;

public class TestEntity(Guid id, string name)
{
    public Guid Id { get; set; } = id;
    public string Name { get; set; } = name;
}
