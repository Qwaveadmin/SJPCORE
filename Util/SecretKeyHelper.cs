using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using SJPCORE.Models.Interface;
using SJPCORE.Models;
using Newtonsoft.Json;


namespace SJPCORE.Util
{
    public class SecretKeyHelper : ISecretKeyHelper
    {
        private readonly DapperContext _context;
        public SecretKeyHelper(DapperContext context)
        {
            _context = context;
        }

        public string GetSecretKey()
        {
            var secretKeyConfig = GlobalParameter.Config.Find(c => c.key == "SECRETKEY");
            return secretKeyConfig?.value;
        }

        public string EncryptString(string plainText)
        {
            var secretKey = GetSecretKey() ?? "19c103a0b278a71e9fdeac2c50284c71";

            byte[] keyBytes = Encoding.UTF8.GetBytes(HashShiftedText(secretKey, 5));

            using var sha256 = SHA256.Create();
            byte[] aesKey = sha256.ComputeHash(keyBytes);

            using var md5 = MD5.Create();
            byte[] aesIv = md5.ComputeHash(keyBytes);

            using var aes = Aes.Create();
            aes.Key = aesKey;
            aes.IV = aesIv;

            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using var memoryStream = new MemoryStream();
            using var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
            using (var streamWriter = new StreamWriter(cryptoStream))
            {
                streamWriter.Write(plainText);
            }

            return Convert.ToBase64String(memoryStream.ToArray());
        }

        public string EncryptString(string plainText, string secretKey = null)
        {
            if (string.IsNullOrEmpty(secretKey))
            {
                secretKey = GetSecretKey();
            }

            byte[] keyBytes = Encoding.UTF8.GetBytes(HashShiftedText(secretKey,5));

            using var sha256 = SHA256.Create();
            byte[] aesKey = sha256.ComputeHash(keyBytes);

            using var md5 = MD5.Create();
            byte[] aesIv = md5.ComputeHash(keyBytes);

            using var aes = Aes.Create();
            aes.Key = aesKey;
            aes.IV = aesIv;

            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using var memoryStream = new MemoryStream();
            using var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
            using (var streamWriter = new StreamWriter(cryptoStream))
            {
                streamWriter.Write(plainText);
            }

            return Convert.ToBase64String(memoryStream.ToArray());
        }

        public string DecryptString(string cipherText)
        {
            try
            {
                var secretKey = GetSecretKey() ?? "19c103a0b278a71e9fdeac2c50284c71";
                byte[] keyBytes = Encoding.UTF8.GetBytes(HashShiftedText(secretKey, 5));
                byte[] buffer = Convert.FromBase64String(cipherText);

                using var sha256 = SHA256.Create();
                byte[] aesKey = sha256.ComputeHash(keyBytes);

                using var md5 = MD5.Create();
                byte[] aesIv = md5.ComputeHash(keyBytes);

                using var aes = Aes.Create();
                aes.Key = aesKey;
                aes.IV = aesIv;
                var decrypt = aes.CreateDecryptor(aes.Key, aes.IV);

                using var memoryStream = new MemoryStream(buffer);
                using var cryptoStream = new CryptoStream(memoryStream, decrypt, CryptoStreamMode.Read);
                using var streamReader = new StreamReader(cryptoStream);

                return streamReader.ReadToEnd();
            }
            catch
            {
                return null;
            }
        }
        public string DecryptString(string cipherText, string secretKey = null)
        {
            try
            {
                if (string.IsNullOrEmpty(secretKey))
                {
                    secretKey = GetSecretKey();
                }
                byte[] keyBytes = Encoding.UTF8.GetBytes(HashShiftedText(secretKey, 5));
                byte[] buffer = Convert.FromBase64String(cipherText);

                using var sha256 = SHA256.Create();
                byte[] aesKey = sha256.ComputeHash(keyBytes);

                using var md5 = MD5.Create();
                byte[] aesIv = md5.ComputeHash(keyBytes);

                using var aes = Aes.Create();
                aes.Key = aesKey;
                aes.IV = aesIv;
                var decrypt = aes.CreateDecryptor(aes.Key, aes.IV);

                using var memoryStream = new MemoryStream(buffer);
                using var cryptoStream = new CryptoStream(memoryStream, decrypt, CryptoStreamMode.Read);
                using var streamReader = new StreamReader(cryptoStream);

                return streamReader.ReadToEnd();
            }
            catch
            {
                return null;
            }
        }

        public static string StaticEncryptString( string secretKey,string plainText)
        {

            if (string.IsNullOrEmpty(secretKey))
            {
                throw new ArgumentNullException(nameof(secretKey));
            }
            byte[] keyBytes = Encoding.UTF8.GetBytes(HashShiftedText(secretKey, 5));

            using var sha256 = SHA256.Create();
            byte[] aesKey = sha256.ComputeHash(keyBytes);

            using var md5 = MD5.Create();
            byte[] aesIv = md5.ComputeHash(keyBytes);

            using var aes = Aes.Create();
            aes.Key = aesKey;
            aes.IV = aesIv;

            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using var memoryStream = new MemoryStream();
            using var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
            using (var streamWriter = new StreamWriter(cryptoStream))
            {
                streamWriter.Write(plainText);
            }

            return Convert.ToBase64String(memoryStream.ToArray());
        }

        public static string StaticDecryptString(string cipherText, string secretKey)
        {
            try
            {
                if (string.IsNullOrEmpty(secretKey))
                {
                    throw new ArgumentNullException(nameof(secretKey));
                }
                byte[] keyBytes = Encoding.UTF8.GetBytes(HashShiftedText(secretKey, 5));
                byte[] buffer = Convert.FromBase64String(cipherText);

                using var sha256 = SHA256.Create();
                byte[] aesKey = sha256.ComputeHash(keyBytes);

                using var md5 = MD5.Create();
                byte[] aesIv = md5.ComputeHash(keyBytes);

                using var aes = Aes.Create();
                aes.Key = aesKey;
                aes.IV = aesIv;
                var decrypt = aes.CreateDecryptor(aes.Key, aes.IV);

                using var memoryStream = new MemoryStream(buffer);
                using var cryptoStream = new CryptoStream(memoryStream, decrypt, CryptoStreamMode.Read);
                using var streamReader = new StreamReader(cryptoStream);

                return streamReader.ReadToEnd();
            }
            catch
            {
                return null;
            }
        }
        public static string HashShiftedText(string input, int shift)
        {
            string shiftedText = ShiftText(input, shift);

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(shiftedText);
                byte[] hashBytes = sha256.ComputeHash(inputBytes);
                return Convert.ToBase64String(hashBytes);
            }
        }

        public static string ShiftText(string input, int shift)
        {
            StringBuilder shiftedText = new StringBuilder();

            foreach (char c in input)
            {
                char shiftedChar = (char)(((int)c + shift - 32) % 95 + 32);
                shiftedText.Append(shiftedChar);
            }

            return shiftedText.ToString();
        }
        public static AuthorizeModel GetAuthorizeModel(HttpContext context)
        {
            var authorizationCookie = context.Request.Cookies["Authorization"];

            if (string.IsNullOrEmpty(authorizationCookie))
            {
                return null; 
            }

            var secretKeyConfig = GlobalParameter.Config.Find(c => c.key == "Secretkey");
            var SecretKeyHelper = new SecretKeyHelper(null);
            string decryptedText = SecretKeyHelper.DecryptString(authorizationCookie, secretKeyConfig.value);

            var authenModel = JsonConvert.DeserializeObject<AuthorizeModel>(decryptedText);
            return authenModel;
        }
        public static AuthorizeModel UpdateAuthorizeModel(HttpContext context, AuthorizeModel newAuthModel)
        {
            var authorizationCookie = context.Request.Cookies["Authorization"];

            if (string.IsNullOrEmpty(authorizationCookie))
            {
                return null;
            }

            var secretKeyConfig = GlobalParameter.Config.Find(c => c.key == "Secretkey");
            var SecretKeyHelper = new SecretKeyHelper(null);
            string decryptedText = SecretKeyHelper.DecryptString(authorizationCookie, secretKeyConfig.value);

            var authenModel = JsonConvert.DeserializeObject<AuthorizeModel>(decryptedText);

            // Get the properties of the newAuthModel using reflection
            var newAuthModelProperties = newAuthModel.GetType().GetProperties();

            // Iterate through the properties and update them in the authenModel
            foreach (var property in newAuthModelProperties)
            {
                var newValue = property.GetValue(newAuthModel);
                if (newValue != null)
                {
                    // Find the corresponding property in the authenModel and update it
                    var authModelProperty = authenModel.GetType().GetProperty(property.Name);
                    if (authModelProperty != null && authModelProperty.CanWrite)
                    {
                        authModelProperty.SetValue(authenModel, newValue);
                    }
                }
            }

            return authenModel;
        }
        public static void SetNewCookie(HttpContext context, AuthorizeModel newAuthModel)
        {
            try
            {
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.Now.Add(TimeSpan.FromMinutes(1440)) // Set the expiration time as needed
                };
                // Remove the existing Authorization cookie
                context.Response.Cookies.Delete("Authorization");

                // Add the new Authorization cookie with the updated value
                context.Response.Cookies.Append("Authorization", StaticEncryptString(GlobalParameter.Config.Find(c => c.key == "SECRETKEY").value, JsonConvert.SerializeObject(newAuthModel)), cookieOptions);
            }
            catch (Exception ex)
            {
                // Handle exceptions if necessary
                Console.WriteLine("Error setting new cookie: " + ex.Message);
            }
        }

        public static string GetUsername(HttpContext context) {
            try
            {
                string cipherText = StaticDecryptString(context.Request.Cookies["Authorization"], GlobalParameter.Config.Find(c => c.key == "SECRETKEY").value);

                
                var username = JsonConvert.DeserializeObject<AuthorizeModel>(cipherText).username;

                return username;
            }
            catch
            {
                return "error";
            }

        }

        public static bool IsSuperAdmin(HttpContext context)
        {
            try
            {
                string cipherText = StaticDecryptString(context.Request.Cookies["Authorization"], GlobalParameter.Config.Find(c => c.key == "SECRETKEY").value);


                var username = JsonConvert.DeserializeObject<AuthorizeModel>(cipherText).username;

                var superAdmin = JsonConvert.DeserializeObject<string[]>(@GlobalParameter.Config.Where(w => w.key == "SUPERADMIN").FirstOrDefault().value);

                if (superAdmin.Contains(username))
                {
                    return true;
                }

                return false;
                
                
            }
            catch
            {
                return false;
            }

        }
        public static int cntSite(HttpContext context)
        {
            try
            {
                string cipherText = StaticDecryptString(context.Request.Cookies["Authorization"], GlobalParameter.Config.Find(c => c.key == "SECRETKEY").value);

                var cntSite = JsonConvert.DeserializeObject<AuthorizeModel>(cipherText).site_allow;

                return cntSite.Count();


            }
            catch
            {
                return 0;
            }

        }
        public static string GetID(HttpContext context)
        {
            try
            {
                string cipherText = StaticDecryptString(context.Request.Cookies["Authorization"], GlobalParameter.Config.Find(c => c.key == "SECRETKEY").value);


                var id = JsonConvert.DeserializeObject<AuthorizeModel>(cipherText).userId;

                return id;
            }
            catch
            {
                return "error";
            }

        }
        public static string GetCurrentSite(HttpContext context)
        {
            try
            {
                string cipherText = StaticDecryptString(context.Request.Cookies["Authorization"], GlobalParameter.Config.Find(c => c.key == "SECRETKEY").value);


                var id = JsonConvert.DeserializeObject<AuthorizeModel>(cipherText).current_site;

                return id;
            }
            catch
            {
                return "error";
            }

        }
        public static string GetFirstname(HttpContext context)
        {
            try
            {
                string cipherText = StaticDecryptString(context.Request.Cookies["Authorization"], GlobalParameter.Config.Find(c => c.key == "SECRETKEY").value);


                var firstname = JsonConvert.DeserializeObject<AuthorizeModel>(cipherText).firstname;

                return firstname;
            }
            catch
            {
                return "error";
            }

        }
        public static string GetLastname(HttpContext context)
        {
            try
            {
                string cipherText = StaticDecryptString(context.Request.Cookies["Authorization"], GlobalParameter.Config.Find(c => c.key == "SECRETKEY").value);


                var lastname = JsonConvert.DeserializeObject<AuthorizeModel>(cipherText).lastname;

                return lastname;
            }
            catch
            {
                return "error";
            }

        }
        public static string GetFullname(HttpContext context)
        {
            try
            {
                string cipherText = StaticDecryptString(context.Request.Cookies["Authorization"], GlobalParameter.Config.Find(c => c.key == "SECRETKEY").value);


                var firstname = JsonConvert.DeserializeObject<AuthorizeModel>(cipherText).firstname;
                var lastname = JsonConvert.DeserializeObject<AuthorizeModel>(cipherText).lastname;

                return firstname +" "+ lastname;
            }
            catch
            {
                return "error";
            }

        }
        public static string[] GetAuthorizeSite(HttpContext context)
        {
            try
            {
                string cipherText = StaticDecryptString(context.Request.Cookies["Authorization"], GlobalParameter.Config.Find(c => c.key == "SECRETKEY").value);


                var sote_allow = JsonConvert.DeserializeObject<AuthorizeModel>(cipherText).site_allow;

                return sote_allow;
            }
            catch
            {
                return new string[0];
            }

        }
    }
}