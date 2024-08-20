using System;
using System.Collections.Generic;

namespace SJPCORE.Models
{
    public class MqttStationModel
    {
        public string[] id { get; set; }
        public string type { get; set; }
        public string id_file { get; set; }
    }

    public class MqttStationModel_Volume
    {
        public string user { get; set; }
        public string cmd { get; set; }
        public string client { get; set; } = "all";
        public int vol { get; set; }
    }

    public class MqttStationModelPLay { 
        public string key { get; set; }
        public string type_user { get; set; }
        public string type_play { get; set; }
        public string key_sound { get; set; }
    }

    public class MqttStationModelMute
    {
        public string key { get; set; }
        public string type_user { get; set; }
        public bool mute { get; set; }
    }
    public class MqttStationModelVolume
    {
        public string key { get; set; }
        public string type_user { get; set; }
        public int vol { get; set; }
    }

    public class MqttStationModelStop
    {
        public string key { get; set; }
        public string type_user { get; set; }
        public string type { get; set; }
    }

    public class MqttStationModelBroadcast
    {
        public string key { get; set; }
        public List<string> key_multi { get; set; }
        public string type_user { get; set; }
        public bool broadcast { get; set; }
    }
    public class MqttStationModelConfig
    {
        public string key { get; set; }
        public string base64json { get; set; }
    }

    public class MqttStationModelWebRTC
    {
        public string user { get; set; }
        public string type_user { get; set; }
        public string cmd { get; set; }
        public string type { get; set; }
        public string message { get; set; }
        public string target { get; set; }
        public List<string> target_multi { get; set; }
    }
}
