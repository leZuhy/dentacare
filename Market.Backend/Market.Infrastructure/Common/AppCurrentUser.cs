using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Market.Application.Abstractions;
using Market.Domain.Entities.Identity;
using Market.Shared.Constants;

namespace Market.Infrastructure.Common;

/// <summary>
/// Implementation of IAppCurrentUser that reads data from a JWT token.
/// </summary>
public sealed class AppCurrentUser(IHttpContextAccessor httpContextAccessor)
    : IAppCurrentUser
{
    private readonly ClaimsPrincipal? _user = httpContextAccessor.HttpContext?.User;

    public int? UserId =>
        int.TryParse(_user?.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : null;

    public string? Email =>
        _user?.FindFirstValue(AppClaimTypes.Email);

    public bool IsAuthenticated =>
        _user?.Identity?.IsAuthenticated ?? false;

    public bool IsAdmin => HasRole(Role.Admin);

    public bool IsDentist => HasRole(Role.Dentist);

    public bool IsPatient => HasRole(Role.Patient);

    private bool HasRole(Role role) =>
                _user?.IsInRole(role.ToString()) ?? false;
}