using Microsoft.EntityFrameworkCore;
using OpenHub.Infrastructure;
using OpenHub.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Postgres")!)
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!);
builder.Services.AddCors(o => o.AddPolicy("web", p => p.WithOrigins(builder.Configuration["WebOrigin"] ?? "https://localhost:7101").AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseExceptionHandler("/error");
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseHttpsRedirection();
app.UseCors("web");
app.MapControllers();
app.MapHealthChecks("/health");
app.MapGet("/error", () => Results.Problem("An unexpected server error occurred."));
if (app.Configuration.GetValue("Database:MigrateOnStartup", false))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<OpenHubDbContext>().Database.MigrateAsync();
}
await app.RunAsync();
public partial class Program;
