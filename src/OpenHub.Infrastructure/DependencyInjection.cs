using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenHub.Application.Abstractions;
using OpenHub.Infrastructure.Persistence;
using OpenHub.Infrastructure.Ranking;
using OpenHub.Infrastructure.Sync;
using Polly;
using StackExchange.Redis;

namespace OpenHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<OpenHubDbContext>(o => o.UseNpgsql(configuration.GetConnectionString("Postgres")));
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(
            configuration.GetConnectionString("Redis") ?? "localhost:6379,abortConnect=false"));
        services.AddSingleton<ISyncQueue, SyncQueue>();
        services.AddScoped<IRankingService, RankingService>();
        services.AddScoped<IGitHubSyncService, GitHubSyncService>();
        services.AddSingleton(TimeProvider.System);
        services.AddHostedService<GitHubSyncWorker>();

        services.AddHttpClient("GitHub", client =>
        {
            client.BaseAddress = new Uri("https://api.github.com/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("OpenHub/1.0");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
            var token = configuration["GitHub:Token"];
            if (!string.IsNullOrWhiteSpace(token)) client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        }).AddStandardResilienceHandler(options =>
        {
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.Delay = TimeSpan.FromSeconds(1);
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(20);
            options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(2);
        });
        return services;
    }
}
