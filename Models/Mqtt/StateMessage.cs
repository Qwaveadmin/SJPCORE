using System;
using System.Collections.Generic;

namespace SJPCORE.Models.Mqtt
{
    public class StateMessage
    {
        public string Id { get; set; }
        public string Cmd { get; set; }
        public int Code { get; set; }
        public List<PlayerStatus> Player { get; set; }
        public DateTime time { get; set; } = DateTime.Now;
    }

    public class PlayerStatus
    {
        public string name { get; set; }
        public bool status { get; set; }
    }
}
