using Microsoft.EntityFrameworkCore;
using WorkerService.Persistence.Entities;

namespace WorkerService.Persistence;

public class WorkerServiceContext(DbContextOptions<WorkerServiceContext> options) : DbContext(options)
{
    public DbSet<TestEntity> TestEntities { get; set; }
}
