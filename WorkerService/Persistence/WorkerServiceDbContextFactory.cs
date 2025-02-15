using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WorkerService.Persistence;

public class WorkerServiceDbContextFactory : IDesignTimeDbContextFactory<WorkerServiceContext>
{
    public WorkerServiceContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<WorkerServiceContext>();
        optionsBuilder.UseSqlServer();

        return new WorkerServiceContext(optionsBuilder.Options);
    }
}