using Dapper;
using System;

namespace SJPCORE.Models.Site
{
    [Table("site_access")]
    public class SiteAccessModel
    {
        public string Id { get; set; }
        public string Users { get; set; }
        public string Site { get; set; } // Change this property to represent the site ID
        public string Role { get; set; }
        public DateTime Create_At { get; set; }
    }
}
