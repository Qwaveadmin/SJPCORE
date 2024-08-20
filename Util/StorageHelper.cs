using System;

namespace SJPCORE.Util
{
    public static class StorageHelper
    {
        public static string GenerateShortGuid(string shortname)
        {
            Guid guid = Guid.NewGuid();
            string shortGuid = shortname+guid.ToString().Substring(0, 6);
            return shortGuid.ToUpper();
        }

        public static string GenerateUuid()
        {
            Guid uuid = Guid.NewGuid();
            return uuid.ToString();
        }
    }
}
