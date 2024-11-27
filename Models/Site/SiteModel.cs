using Dapper;
using System;

namespace SJPCORE.Models.Site
{
    [Table("site_all")]
    public class SiteModel
    {
        [Key]
        public string id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string user_update { get; set; }
        public string user_create { get; set; }
        public DateTime update_at { get; set; }
        public DateTime create_at { get; set; }
    }
}