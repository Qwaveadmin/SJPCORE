using Dapper;
using System;
using System.Collections.Generic;

namespace SJPCORE.Models
{
    [Table("sjp_grp")]
    public class GroupModel
    {
        public int id { get; set; } // Auto Increment
        [Key]
        public string key { get; set; } = SJPCORE.Util.StorageHelper.GenerateShortGuid("GRP");
        public string name { get; set; }
        public bool mute { get; set; } = false;
        public int vol { get; set; } = 30;
        public DateTime update_at { get; set; } = DateTime.Now;
        public DateTime create_at { get; set; } = DateTime.Now;

    }

    public class _GroupModel
    {
        public string key { get; set; }
        public string name { get; set; }
        public string[] nodes { get; set; }
        public int vol { get; set; }
        public GroupModel group_info { get; set; }
    }
    
    public class _ManageGroupModel
    {
        public string key { get; set; }
        public string name { get; set; }
        
        public List<_GroupModel> nodes { get; set; }
        public List<StationModel> station_info { get; set; }
        public GroupModel group_info { get; set; }

    }
}
