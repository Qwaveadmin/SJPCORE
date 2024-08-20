using Dapper;
using System;

namespace SJPCORE.Models
{
    [Table("sjp_schedule")]
    public class ScheduleModel
    {
        public int id { get; set; }
        [Key]
        public string key { get; set; } = SJPCORE.Util.StorageHelper.GenerateShortGuid("SCH");
        public string type_play { get; set; }
        public string id_play { get; set; }
        public string type_station { get; set; }
        public string key_station { get; set; }
        public DateTime? schtime { get; set; }
        public int? duration { get; set; }
        public bool once_time { get; set; } = false;
        public bool every_hour { get; set; } = false;
        public bool every_day { get; set; } = false;
        public bool every_week { get; set; } = false;
        public DateTime? lastest_played { get; set; }
        public DateTime timestamp_create { get; set; } = DateTime.Now;

    }

    public class AddScheduleRequest
    {
        public string type_station { get; set; }
        public string key_station { get; set; }
        public string type_play { get; set; }
        public string id_play { get; set; }
        public DateTime schtime { get; set; }
        public int duration { get; set; }
        public string repeat { get; set; }
    }

    public class SchStationModel
    {
        public string name { get; set; }
        public string type { get; set; }
        public string key { get; set; }

    }
}
