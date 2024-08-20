using System.Text.RegularExpressions;
using System;

namespace SJPCORE.Models.Mqtt
{
    public class MediaMessage
    {
        public string Id { get; set; }
        public int Code { get; set; }
        public DateTime Time { get; set; }
        public string Type { get; set; }
        public string Cmd { get; set; }
        public string Key { get; set; }

        public enum CommandCategory
        {
            Play,
            Stop,
            Volume,
            Mute,
            Unknown
        }

        public CommandCategory Category
        {
            get
            {
                switch (Cmd.ToLower())
                {
                    case "play":
                        return CommandCategory.Play;
                    case "stop":
                        return CommandCategory.Stop;
                    case var s when s.StartsWith("ch-vol"):
                        return CommandCategory.Volume;
                    case var s when s.StartsWith("mute"):
                        return CommandCategory.Mute;
                    default:
                        return CommandCategory.Unknown;
                }
            }
        }

        public int VolumeValue
        {
            get
            {
                if (Category == CommandCategory.Volume)
                {
                    var match = Regex.Match(Cmd, @"ch-vol \((\d+)\)");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out int value))
                    {
                        return value;
                    }
                }
                return 0;
            }
        }

        public bool MuteValue
        {
            get
            {
                if (Category == CommandCategory.Mute)
                {
                    var match = Regex.Match(Cmd, @"mute \((True|False)\)");
                    if (match.Success && bool.TryParse(match.Groups[1].Value, out bool value))
                    {
                        return value;
                    }
                }
                return false;
            }
        }
    }
}
