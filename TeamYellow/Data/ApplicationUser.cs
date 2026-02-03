using Microsoft.AspNetCore.Identity;
using TeamYellow.Models;

namespace TeamYellow.Data;

public class ApplicationUser : IdentityUser
{
    public virtual Counsellor? Counsellor { get; set; }
}
