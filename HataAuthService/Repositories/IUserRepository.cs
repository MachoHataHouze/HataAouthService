using HataAuthService.Models;

namespace HataAuthService.Repositories;

public interface IUserRepository
{
    Task<User> GetByEmailAsync(string email);
    Task AddAsync(User user);
}
