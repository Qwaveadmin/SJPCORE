using SJPCORE.Models;
using System.Collections.Generic;

namespace SJPCORE
{
    public static class GlobalParameter
    {
        public static List<ConfigModel> Config { get; set; }

        public static string topic { get; set; } = "/sub/node";
        public static string client { get; set; } = "commander";
        public static string host { get; set; } = "mqtt-server.qwave.cloud";
        public static string port { get; set; } = "11257";
        public static string username { get; set; } = "QWAVE";
        public static string password { get; set; } = "QWAVE";
    }
}
