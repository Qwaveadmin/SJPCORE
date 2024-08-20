using Dapper;
using System;

namespace SJPCORE.Models
{
    [Table("sjp_setting")]
    public class ConfigModel
    {
        
        public int id { get; set; }
        [Key]
        public string key { get; set; }
        public string value { get; set; }
        public string grp { get; set; }
        public DateTime update_at { get; set; }
    }
}
