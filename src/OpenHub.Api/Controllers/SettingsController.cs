using Microsoft.AspNetCore.Mvc;
using OpenHub.Application.Abstractions;
using OpenHub.Application.Contracts;

namespace OpenHub.Api.Controllers;

[ApiController, Route("api/settings")]
public sealed class SettingsController(IGitHubSyncService sync) : ControllerBase
{
    [HttpPost("github/test")]
    public Task<ConnectionStatus> TestGitHub(CancellationToken ct) => sync.TestConnectionAsync(ct);
}
