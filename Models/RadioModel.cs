using Dapper;
using System;

namespace SJPCORE.Models
{
    [Table("sjp_radio")]
    public class RadioModel
    {

        [Key]
        public int id { get; set; }
        public string name { get; set; }
        public string url { get; set; }
        public DateTime update_at { get; set; }
        public DateTime create_at { get; set; }

    }
    public class RadioBody
    {
        public string name { get; set; }
        public string url { get; set; }
    }
}
