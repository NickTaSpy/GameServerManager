using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Principal;
using System.Text;

namespace GameServerManager.Server.Helpers
{
    public static class TokenHelper
    {
        public static bool ValidateToken(string authToken, string key)
        {
            var validationParameters = GetValidationParameters(key);
            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                _ = tokenHandler.ValidateToken(authToken, validationParameters, out SecurityToken validatedToken);
            }
            catch
            {
                return false;
            }

            return true;
        }

        private static TokenValidationParameters GetValidationParameters(string key) => new()
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
        };
    }
}
