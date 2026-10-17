using Climato.Gateway.Application.Contracts;
using Climato.Gateway.Application.Core.Mappings;
using Climato.Gateway.Application.Services;
using Climato.Gateway.Domain.Cities;
using Climato.Gateway.Domain.Users;
using Climato.Gateway.Infrastructure.Data.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddAutoMapper(typeof(ApplicationAutoMapperProfile).Assembly);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddTransient<ICityRepository, CityRepository>();
builder.Services.AddTransient<ICityAppService, CityAppService>();
builder.Services.AddTransient<IWeatherAppService, WeatherAppService>();
builder.Services.AddHttpClient<IWeatherAppService, WeatherAppService>();
builder.Services.AddTransient<IUserRepository, UserRepository>();
builder.Services.AddTransient<IUserAppService, UserAppService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseCors("AllowAll");

app.MapControllers();

app.Run();
