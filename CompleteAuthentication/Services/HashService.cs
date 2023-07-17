using System.Security.Cryptography;
using System.Text;

namespace CompleteAuthentication.Services
{
    public class HashService
    {
        public static string HashPassword(string password)
        {
            using SHA256 sHA256 = SHA256.Create();
            byte[] hashedBytes = sHA256.ComputeHash(Encoding.UTF8.GetBytes(password));
            string hasedPassword = BitConverter.ToString(hashedBytes).Replace("-","").ToLower();
            return hasedPassword;
        }
    }
}
