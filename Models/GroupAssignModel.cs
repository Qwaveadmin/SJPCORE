using Dapper;
using System;

namespace SJPCORE.Models
{
    [Table("sjp_grp_assign")]
    public class GroupAssignModel
    {
        [Key]
        public string grp_id { get; set; } 
        public string station_id { get; set; }

    }

    public class _GroupAssignModel
    {
        public string name { get; set; }
        public string[] nodes { get; set; }

    }
}
