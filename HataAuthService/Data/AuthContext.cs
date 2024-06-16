using HataAuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace HataAuthService.Data;

public class AuthContext : DbContext
{
    public AuthContext(DbContextOptions<AuthContext> options) : base(options) { }
    public DbSet<User> Users { get; set; }
}