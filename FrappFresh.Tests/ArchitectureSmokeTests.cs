using FrappFresh.Infrastructure.Persistence;

namespace FrappFresh.Tests;

public sealed class ArchitectureSmokeTests
{
    [Fact]
    public void AppDbContext_IsAvailableFromInfrastructureLayer()
    {
        Assert.Equal("FrappFresh.Infrastructure", typeof(AppDbContext).Assembly.GetName().Name);
    }
}
