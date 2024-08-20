using Dapper;
using System;

namespace SJPCORE.Models
{
    [Table("sjp_storage")]
    public class StorageModel
    {

        public int id { get; set; }
        public string category { get; set; }
        [Key]
        public string key { get; set; }
        public string name_defualt { get; set; }
        public string name_server { get; set; }
        public string path { get; set; }
        public string size { get; set; }
        public int upload_by { get; set; }
        public DateTime update_at { get; set; }
        public DateTime create_at { get; set; }
    }
}
