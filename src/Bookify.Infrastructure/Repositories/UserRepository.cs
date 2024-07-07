using Bookify.Domain.Users;

namespace Bookify.Infrastructure.Repositories;

internal sealed class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext dbContext)
        : base(dbContext)
    {
    }


    // to override we need to make that virtual , and we can modify it when ever we want
    // as we don't use the base so we need to write  DbContext.Add(user); again
    
    public override void Add(User user)
    {
        // we do this because we have role in db but ef could insert it as new obj ,as we are working untrack way
        // so we need to add in manually

        foreach (var role in user.Roles)
        {
            DbContext.Attach(role); // this tells ef ,any roles are existed in db and no need to insert it in db as new.

        }

        DbContext.Add(user);
    }
}