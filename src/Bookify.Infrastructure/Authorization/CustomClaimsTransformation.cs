using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using Bookify.Infrastructure.Authentication;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Bookify.Infrastructure.Authorization;

// add claims
internal sealed class CustomClaimsTransformation : IClaimsTransformation
{
    private readonly IServiceProvider _serviceProvider;

    public CustomClaimsTransformation(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not { IsAuthenticated: true } ||
            principal.HasClaim(claim => claim.Type == ClaimTypes.Role) &&
            principal.HasClaim(claim => claim.Type == JwtRegisteredClaimNames.Sub))
        {
            return principal;
        }


        // adding role claims
        // we want to get user roles from AuthorizationService, so we need to register it first and then call
        // this method  GetRolesForUserAsync
        using var scope = _serviceProvider.CreateScope();

        var authorizationService = scope.ServiceProvider.GetRequiredService<AuthorizationService>();

        var identityId = principal.GetIdentityId(); // user Id

         // this is the disadvantage of using this approach but we will use redis to solve this issue
        var userRoles = await authorizationService.GetRolesForUserAsync(identityId); // get roles

        var claimsIdentity = new ClaimsIdentity(); // assigning to the claims

        claimsIdentity.AddClaim(new Claim(JwtRegisteredClaimNames.Sub, userRoles.UserId.ToString())); // add userId to claim

        foreach (var role in userRoles.Roles) // adding roles to the claims
        {
            claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, role.Name));
        }

        principal.AddIdentity(claimsIdentity); // add claims

        return principal;
    }
}
