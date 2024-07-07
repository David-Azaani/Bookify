using Microsoft.AspNetCore.Authorization;

namespace Bookify.Infrastructure.Authorization;


// Make Custom attribute
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission)
        : base(permission) // hover on base now we can change [Authorize("users:read")] >> [hasPermission("users:read")]
    {
    }
}