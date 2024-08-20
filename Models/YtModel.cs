using Dapper;
using System;

namespace SJPCORE.Models
{
    [Table("sjp_youtube")]
    public class YtModel
    {
        [Key]
        public int id { get; set; }
        public string name { get; set; }
        public string url { get; set; }
        public DateTime update_at { get; set; }
        public DateTime create_at { get; set; }
    }
    public class YtBody
    {
        public string name { get; set; }
        public string url { get; set; }
    }
}
