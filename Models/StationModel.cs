using Dapper;
using System;

namespace SJPCORE.Models
{
    [Table("sjp_station")]
    public class StationModel
    {

        [Key]
        public int? id { get; set; }
        public string key { get; set; } = SJPCORE.Util.StorageHelper.GenerateShortGuid("STA");
        public string name { get; set; }
        public bool active { get; set; } = true;
        public bool mute { get; set; } = true;
        public int vol { get; set; } = 30;
        public string status { get; set; } = "ไม่มีข้อมูล";
        public bool status_media { get; set; } = false;
        public bool status_stream { get; set; } = false;
        public bool status_schedule { get; set; } = false;
        public string description { get; set; } = "";
        public DateTime update_at { get; set; } = DateTime.Now;
        public DateTime create_at { get; set; } = DateTime.Now;

    }
}
