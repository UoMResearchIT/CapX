using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using PPMTool.Data.Entities;

namespace PPMTool.Data
{
    public sealed class ApiTokenProvider
    {
        private IConfiguration configuration;

        public ApiTokenProvider(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        /// <summary>
        /// Method to create a JWT by encoding claims
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public string CreateToken(User user)
        {
            string secretKey = configuration["Jwt:SecretKey"];
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("UserId", user.UserId.ToString()),
                    new Claim("RoleType", user.RoleType.ToString())
                }),
                Expires = System.DateTime.UtcNow.AddDays(configuration.GetValue<double>("Jwt:ExpirationInDays")),
                SigningCredentials = credentials
            };

            var handler = new JsonWebTokenHandler();
            var key = handler.CreateToken(tokenDescriptor);

            Debug.WriteLine($"Created key for {user.Name} => {key}");

            return key;
        }
    }
}
