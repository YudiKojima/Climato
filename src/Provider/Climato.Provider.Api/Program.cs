using Climato.Provider.Api;
using Climato.Provider.Api.Observability;
using Climato.Provider.Application;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability();
builder.Services
    .AddProviderApi(builder.Configuration)
    .AddProviderApplication(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseSerilogRequestLogging(options => options.GetLevel = RequestLogLevel.Get);
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHttpsRedirection();

app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.Run();
