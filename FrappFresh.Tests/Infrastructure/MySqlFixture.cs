using FrappFresh.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MySqlConnector;

namespace FrappFresh.Tests.Infrastructure;

public class MySqlFactAttribute : FactAttribute
{
    public MySqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("FRAPPFRESH_TEST_MYSQL")))
            Skip = "Configura FRAPPFRESH_TEST_MYSQL para ejecutar contra un MySQL real.";
    }
}

[CollectionDefinition("MySql")]
public class MySqlCollection : ICollectionFixture<MySqlFixture>;

public class MySqlFixture : IAsyncLifetime
{
    public string ConnectionString { get; private set; } = string.Empty;
    private ServerVersion _version = new MySqlServerVersion(new Version(8, 0, 36));

    public async Task InitializeAsync()
    {
        var connection = Environment.GetEnvironmentVariable("FRAPPFRESH_TEST_MYSQL");
        if (string.IsNullOrWhiteSpace(connection)) return;
        var builder = new MySqlConnectionStringBuilder(connection)
        {
            Database = "FFTests_" + Guid.NewGuid().ToString("N")
        };
        ConnectionString = builder.ConnectionString;
        // Detectar sin seleccionar una base que todavía no existe.
        var serverConnection = new MySqlConnectionStringBuilder(connection) { Database = "" }.ConnectionString;
        _version = ServerVersion.AutoDetect(serverConnection);
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public AppDbContext CreateContext(params IInterceptor[] interceptors)
    {
        var builder = new DbContextOptionsBuilder<AppDbContext>().UseMySql(ConnectionString, _version);
        if (interceptors.Length > 0) builder.AddInterceptors(interceptors);
        return new AppDbContext(builder.Options);
    }

    public async Task DisposeAsync()
    {
        if (string.IsNullOrEmpty(ConnectionString)) return;
        // Solo se borra la base temporal generada por esta fixture.
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
    }
}
