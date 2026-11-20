using System.Text.Json.Serialization;
using ClaimsApi.Data;
using ClaimsApi.Features.Claims;
using ClaimsApi.Features.Dashboard;
using ClaimsApi.Infrastructure.Kafka;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<ClaimsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ClaimsDb")));

// Kafka: one shared publisher, plus the background consumer that feeds the dashboard.
builder.Services.Configure<KafkaOptions>(builder.Configuration.GetSection("Kafka"));
builder.Services.AddSingleton<ClaimEventPublisher>();
builder.Services.AddHostedService<ClaimEventsConsumer>();

// Let the Angular dev server (a different origin) call this API from the browser.
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy
        .WithOrigins("http://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod()));

// Send and accept enums as text ("Motor") in JSON, not numbers.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));

var app = builder.Build();

// Create or update the database tables on startup, so nobody has to run
// "dotnet ef database update" by hand. Runs before the Kafka consumer starts.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<ClaimsDbContext>().Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();

app.MapClaimEndpoints();
app.MapDashboardEndpoints();

app.Run();
