using Climato.Gateway.Api;
using Climato.Gateway.Application;
using Climato.Gateway.Infrastructure;
using Climato.Gateway.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddGatewayApi(builder.Configuration)
    .AddGatewayApplication()
    .AddGatewayInfrastructure(builder.Configuration);

var app = builder.Build();

if (!app.Environment.IsProduction())
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ClimatoDbContext>();
    await dbContext.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHttpsRedirection();
app.UseCors("Web");
app.UseAuthorization();

app.MapControllers();

app.Run();
