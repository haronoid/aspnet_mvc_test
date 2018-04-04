using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.Identity;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Owin;

[assembly: OwinStartup(typeof(MvcWeb2.OwinStartUp))]
namespace MvcWeb2
{
    public class OwinStartUp
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                LoginPath = new PathString("/Auth/Login")
            });
        }
    }

    public class AppUser : IUser
    {
        public string Id { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }
    }

    public class AppUserStore : IUserStore<AppUser>, IUserPasswordStore<AppUser>
    {
        private static List<AppUser> users = new List<AppUser>
        {
            new AppUser { Id = "1", UserName = "user1", Password="abc" },
            new AppUser { Id = "2", UserName = "user2", Password="def" }
        };

        public Task CreateAsync(AppUser user)
        {
            users.Add(user);
            return Task.Delay(0);
        }

        public Task DeleteAsync(AppUser user)
        {
            return Task.Delay(0);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Task<AppUser> FindByIdAsync(string userId)
        {
            return Task.FromResult(users.FirstOrDefault(u => u.Id == userId));
        }

        public Task<AppUser> FindByNameAsync(string userName)
        {
            return Task.FromResult(users.FirstOrDefault(u => u.UserName == userName));
        }

        public Task<string> GetPasswordHashAsync(AppUser user)
        {
            return Task.FromResult(new PasswordHasher().HashPassword(user.Password));
        }

        public Task<bool> HasPasswordAsync(AppUser user)
        {
            return Task.FromResult(true);
        }

        public Task SetPasswordHashAsync(AppUser user, string passwordHash)
        {
            return Task.Delay(0);
        }

        public async Task UpdateAsync(AppUser user)
        {
            var target = await this.FindByIdAsync(user.Id);
            if (target == null)
            {
                return;
            }
            target.UserName = user.UserName;
        }
    }
}