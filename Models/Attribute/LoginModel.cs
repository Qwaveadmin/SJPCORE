using Dapper;

namespace SJPCORE.Models.Attribute
{
    public class LoginModel
    {
        public string Username { get; set; }
        public string Password { get; set; }


    }

    [Table("sjp_user")]
    public class UserModel
    {
        public string Id { get; set; }
        [Key]
        public string Username { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
    }

    [Table("sjp_role")]
    public class UserRoleModel
    {
        public string Id { get; set; }
        public string Role { get; set; }
    }
}
