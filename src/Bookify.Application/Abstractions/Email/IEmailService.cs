namespace Bookify.Application.Abstractions.Email;

public interface IEmailService
{
    //? no need to make it a value object for this because we don have any value
    Task SendAsync(Domain.Users.Email recipient, string subject, string body);

}