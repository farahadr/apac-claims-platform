using System.Text.Json.Serialization;
using ClaimsApi.Data;
using ClaimsApi.Features.Claims;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<ClaimsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ClaimsDb")));

// Send and accept enums as text ("Motor") in JSON, not numbers.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapClaimEndpoints();

app.Run();
