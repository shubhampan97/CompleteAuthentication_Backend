using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CompleteAuthentication.Services
{
    public class TokenService
    {
        public static string CreateAccessToken(int id, string accessKey)
        {
            List<Claim> claims = new()
            {
                new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            };

            SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(accessKey));
            SigningCredentials creds = new(key, SecurityAlgorithms.HmacSha256);
            JwtSecurityToken token = new(claims:claims, notBefore: DateTime.UtcNow, expires:DateTime.UtcNow.AddSeconds(30), signingCredentials:creds);
            string jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return jwt;
        }

        public static string CreateRefreshToken(int id, string refreshKey)
        {
            List<Claim> claims = new()
            {
                new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            };

            SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(refreshKey));
            SigningCredentials creds = new(key, SecurityAlgorithms.HmacSha256);
            JwtSecurityToken token = new(claims: claims, notBefore: DateTime.UtcNow, expires: DateTime.UtcNow.AddDays(7), signingCredentials: creds);
            string jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return jwt;
        }

        public static int DecodeToken(string? token, out bool hasTokenExpired)
        {
            JwtSecurityToken? jwtToken = new(token);
            int id = int.Parse(jwtToken.Claims.First(claim => ClaimTypes.NameIdentifier==claim.Type).Value);
            hasTokenExpired = jwtToken.ValidTo < DateTime.UtcNow;
            return id;
        }
    }
}
