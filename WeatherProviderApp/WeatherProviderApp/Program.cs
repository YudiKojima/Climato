using WeatherProviderApp.Application.Contracts;
using WeatherProviderApp.Application.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddTransient<IWeatherAppService, WeatherAppService>();
builder.Services.AddHttpClient<IWeatherAppService, WeatherAppService>();
builder.Services.AddTransient<ICityImageAppService, CityImageAppService>();
builder.Services.AddHttpClient<ICityImageAppService, CityImageAppService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
