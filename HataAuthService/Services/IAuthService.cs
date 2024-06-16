using HataAuthService.Models;

namespace HataAuthService.Services;

public interface IAuthService
{
    Task RegisterAsync(User user, string password);
    Task<string> AuthenticateAsync(string email, string password);
}