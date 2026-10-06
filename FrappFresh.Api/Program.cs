using FrappFresh.Application;
using FrappFresh.Infrastructure;
using FrappFresh.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();
await app.Services.InitializeDatabaseAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
