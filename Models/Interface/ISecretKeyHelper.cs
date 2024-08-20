namespace SJPCORE.Models.Interface
{
    public interface ISecretKeyHelper
    {
        string EncryptString(string plainText);
        string EncryptString(string plainText, string secretKey = null);
        string DecryptString(string cipherText);
        string DecryptString(string cipherText, string secretKey);
        string GetSecretKey();
    }
}