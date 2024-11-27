using System;
using System.Collections.Generic;

namespace SJPCORE.Models.Site
{
    public class SitePermissionInfoModel
    {
        public class RoleModel
        {
            public string Id { get; set; }
            public string Role { get; set; }
            public DateTime CreateAt { get; set; }
        }
        public class UserModel
        {
            public string Id { get; set; }
            public string Username { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
        }
        public class SiteModel
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }

        public List<RoleModel> Roles { get; set; }
        public List<UserModel> Users { get; set; }
        public List<SiteModel> Sites { get; set; }
    }

}
