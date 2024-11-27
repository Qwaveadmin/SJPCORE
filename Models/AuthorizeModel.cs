using System;
using System.Collections.Generic;
using SJPCORE.Models.Site;

namespace SJPCORE.Models
{
    public class AuthorizeModel
    {
        public Guid seed { get; set; } = Guid.NewGuid();
        public string userId { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string firstname { get; set; }
        public string lastname { get; set; }
        public string authorizeId { get; set; }
        public string current_site { get; set; }
        public List<SiteAccessModel> site_access { get; set; }
        public DateTime create_at { get; set; } = DateTime.Now;
        public DateTime expire_at { get; set; } = DateTime.Now.AddDays(3);

    }
}