namespace Bookify.Domain.Users;

public sealed class Role
{
    // pre determine role
    public static readonly Role Registered = new(1, "Registered");

    public Role(int id, string name)
    {
        Id = id;
        Name = name;
    }

    public int Id { get; init; }

    public string Name { get; init; } = String.Empty;

    // Nav Property
    public ICollection<User> Users { get; init; } = new List<User>();

    // Nav Property
    public ICollection<Permission> Permissions { get; init; } = new List<Permission>();
}
