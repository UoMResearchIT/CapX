using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;

namespace PPMTool.API.Services
{
    public class APIAuthService
    {
        /// <summary>
        /// Expires active keys that match. Returns the user of active key that matches.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="keyValue"></param>
        /// <returns></returns>
        public User? GetUserIfApiKeyActive(PPMToolContext context, string? keyValue)
        {
            if (string.IsNullOrWhiteSpace(keyValue))
            {
                return null;
            }

            var key = context.ApiKeys.Include(x => x.Owner).FirstOrDefault(x => x.Key == keyValue);

            if (key != null && key.Active)
            {
                if (key.ExpiresAt < DateTime.UtcNow)
                {
                    key.Active = false;
                    context.Update(key);
                    context.SaveChanges();
                }

                return key.Owner;
            }

            return null;
        }
    }
}
