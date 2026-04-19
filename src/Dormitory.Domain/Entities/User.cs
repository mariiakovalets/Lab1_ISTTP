using Microsoft.AspNetCore.Identity;

namespace Dormitory.Domain.Entities;

public class User : IdentityUser
{
    public string? FullName { get; set; }
    public int? StudentId { get; set; }
}