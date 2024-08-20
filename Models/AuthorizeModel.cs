using System;

namespace SJPCORE.Models
{
    public class AuthorizeModel
    {
        public Guid seed { get; set; } = Guid.NewGuid();
        public string userId { get; set; }
        public string username { get; set; }
        public string firstname { get; set; }
        public string lastname { get; set; }
        public string authorizeId { get; set; }
        public string current_site { get; set; }
        public string[] site_allow { get; set; }
        public DateTime create_at { get; set; } = DateTime.Now;
        public DateTime expire_at { get; set; } = DateTime.Now.AddDays(3);

    }
}