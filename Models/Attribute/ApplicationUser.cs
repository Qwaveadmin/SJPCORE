using Microsoft.AspNetCore.Identity;

namespace SJPCORE.Models.Attribute
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}
