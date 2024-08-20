using System;
using System.Configuration;
using System.Security.Cryptography;
using System.Management;
using System.Text;
using System.IO;

namespace SJPCORE
{
    public static class HwidManager
    {
        public static string GetHardwareID()
        {
            string hwid = string.Empty;
            ManagementClass mc = new ManagementClass("Win32_ComputerSystemProduct");
            ManagementObjectCollection moc = mc.GetInstances();
            foreach (ManagementObject mo in moc)
            {
                hwid = mo.Properties["UUID"].Value.ToString();
                break;
            }
            return hwid;
        }

        // Generate a unique key based on the HWID
        public static string GetUniqueKey(string hwid)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(hwid);
            byte[] hash = SHA256.Create().ComputeHash(bytes);
            string key = Convert.ToBase64String(hash);
            return key;
        }

        public static void LockHWID()
        {
            if (ConfigurationManager.AppSettings["LockedHWID"] != null)
            {
                return; // Already locked
            }
            string currentHWID = GetHardwareID();
            string lockedHWID = GetUniqueKey(currentHWID);
            // Save the lockedHWID to the configuration file
            ConfigurationManager.RefreshSection("appSettings");
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings["LockedHWID"].Value = lockedHWID;
            config.Save(ConfigurationSaveMode.Modified);
        }

        public static void SaveLockedHWIDToFile(string lockedHWID, string filePath, string password)
        {
            byte[] key = new byte[32]; // AES-256 key size
            byte[] iv = new byte[16]; // AES-256 IV size
            byte[] hwidBytes = Encoding.UTF8.GetBytes(lockedHWID);
            byte[] encryptedBytes = null;
            byte[] salt = new byte[16]; // Salt for key derivation

            // Generate a random salt
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }

            // Derive a key and IV from the password and salt using PBKDF2
            using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, 1000))
            {
                key = deriveBytes.GetBytes(32);
                iv = deriveBytes.GetBytes(16);
            }

            // Encrypt the HWID bytes using AES-256 CBC mode
            using (var aes = Aes.Create())
            {
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = key;
                aes.IV = iv;

                using (var encryptor = aes.CreateEncryptor())
                {
                    using (var ms = new MemoryStream())
                    {
                        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        {
                            cs.Write(hwidBytes, 0, hwidBytes.Length);
                            cs.FlushFinalBlock();
                            encryptedBytes = ms.ToArray();
                        }
                    }
                }
            }

            // Save the encrypted bytes and salt to a file
            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                fs.Write(salt, 0, salt.Length);
                fs.Write(encryptedBytes, 0, encryptedBytes.Length);
            }
        }

        // Check if the current HWID matches the locked HWID in the configuration file
        public static bool IsHardwareIDMatched()
        {
            string lockedHWID = ConfigurationManager.AppSettings["LockedHWID"];
            if (lockedHWID == null)
            {
                return false; // Locked HWID is not set
            }
            string currentHWID = GetHardwareID();
            string currentKey = GetUniqueKey(currentHWID);
            return (currentKey == lockedHWID);
        }

        public static string LoadLockedHWIDFromFile(string filePath, string password)
        {
            byte[] key = new byte[32]; // AES-256 key size
            byte[] iv = new byte[16]; // AES-256 IV size
            byte[] encryptedBytes = null;
            byte[] salt = new byte[16];

            // Read the salt and encrypted bytes from the file
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                fs.Read(salt, 0, salt.Length);
                encryptedBytes = new byte[fs.Length - salt.Length];
                fs.Read(encryptedBytes, 0, encryptedBytes.Length);
            }

            // Derive a key and IV from the password and salt using PBKDF2
            using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, 1000))
            {
                key = deriveBytes.GetBytes(32);
                iv = deriveBytes.GetBytes(16);
            }

            // Decrypt the encrypted bytes using AES-256 CBC mode
            using (var aes = Aes.Create())
            {
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = key;
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor())
                {
                    using (var ms = new MemoryStream(encryptedBytes))
                    {
                        using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        {
                            using (var sr = new StreamReader(cs))
                            {
                                return sr.ReadToEnd();
                            }
                        }
                    }
                }
            }
        }
    }
}