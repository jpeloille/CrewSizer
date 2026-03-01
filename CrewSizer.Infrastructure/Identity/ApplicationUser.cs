using Microsoft.AspNetCore.Identity;

namespace CrewSizer.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string? NomComplet { get; set; }
}
