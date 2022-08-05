using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using System;
using PPMTool.Data;
using System.Linq;

namespace PPMTool.Areas.Identity.Data
{
    public class IdentitySeed
    {
        private PPMToolContext _context;

        public IdentitySeed(PPMToolContext context)
        {
            _context = context;
        }

        public async Task SeedSuperUserAsync()
        {
            // Create superuser
            var user = new PPMToolUser
            {
                UserName = "mobile@manchester.ac.uk",
                NormalizedUserName = "MOBILE@MANCHESTER.AC.UK",
                Email = "mobile@manchester.ac.uk",
                NormalizedEmail = "MOBILE@MANCHESTER.AC.UK",
                Name = "MDS",
                Role = RoleType.Superuser,
                EmailConfirmed = true,
                LockoutEnabled = false,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            // Add superuser to user table if not already there
            if (!_context.Users.Any(u => u.UserName == user.UserName))
            {
                var password = new PasswordHasher<PPMToolUser>();
                var hashed = password.HashPassword(user, "Temp123%");
                user.PasswordHash = hashed;
                var userStore = new UserStore<PPMToolUser>(_context);
                await userStore.CreateAsync(user);
            }

            await _context.SaveChangesAsync();
        }
    }
}
