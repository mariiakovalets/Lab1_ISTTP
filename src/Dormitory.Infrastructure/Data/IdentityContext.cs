using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Dormitory.Domain.Entities;

namespace Dormitory.Infrastructure.Data;

public class IdentityContext : IdentityDbContext<User>
{
    public IdentityContext(DbContextOptions<IdentityContext> options)
        : base(options)
    {
        Database.EnsureCreated();
    }
}