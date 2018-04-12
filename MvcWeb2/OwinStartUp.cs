using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Owin;

[assembly: OwinStartup(typeof(MvcWeb2.OwinStartUp))]
namespace MvcWeb2
{
    public class OwinStartUp
    {
        public void Configuration(IAppBuilder app)
        {
            // 後から「HttpContext.GetOwinContext().Get」で取り出して使える。
            app.CreatePerOwinContext<AppUserStore>(() => new AppUserStore());
            app.CreatePerOwinContext<AppUserManager>((options, context) 
                => new AppUserManager(context.Get<AppUserStore>()));
            app.CreatePerOwinContext<AppRoleManager>((options, context) 
                => new AppRoleManager(context.Get<AppUserStore>()));
            app.CreatePerOwinContext<AppSignInManager>((options, context) 
                => new AppSignInManager(context.GetUserManager<AppUserManager>(), context.Authentication));

            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                LoginPath = new PathString("/Auth/Login")
            });
        }
    }

    public class AppUser : IUser<string>
    {
        private string _id = Guid.NewGuid().ToString();
        private string _userName;
        private string _password;

        public string Id { get => _id; set => _id = value; }
        public string UserName { get => _userName; set => _userName = value; }
        public string Password { get => _password; set => _password = value; }
    }

    public class AppUserManager : UserManager<AppUser>
    {
        public AppUserManager(IUserStore<AppUser> store)
            : base(store)
        { }

        public AppUserManager Create(IUserStore<AppUser> store)
        {
            return new AppUserManager(store);
        }
    }

    public class AppRole : IRole<string>
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Name { get; set ; }
    }

    public class AppRoleManager : RoleManager<AppRole>
    {
        public AppRoleManager(IRoleStore<AppRole, string> store) 
            : base(store)
        { }
    }

    public class AppSignInManager : SignInManager<AppUser, string>
    {
        public AppSignInManager(UserManager<AppUser, string> userManager, IAuthenticationManager authenticationManager)
            : base(userManager, authenticationManager)
        { }
    }


    public class AppUserStore : 
        IUserStore<AppUser>, 
        IUserStore<AppUser,string>,
        IUserPasswordStore<AppUser,string>,
        IUserRoleStore<AppUser, string>,
        IRoleStore<AppRole, string>
    {
        // DBの代わりにダミーデータ設定
       
        // ユーザマスタ
        private static List<AppUser> Users { get; } = new List<AppUser>
        {
            new AppUser { Id = "1", UserName = "user1", Password="abc" },
            new AppUser { Id = "2", UserName = "user2", Password="def" }
        };

        // ロールマスタ
        private static List<AppRole> Roles { get; } = new List<AppRole>
        {
            new AppRole { Id = "1", Name = "emploryee"},
            new AppRole { Id = "999", Name = "admin" }
        };

        // ユーザー・ロールマスタ
        private static List<Tuple<string, string>> UserRoleMap { get; } = new List<Tuple<string, string>>
        {
            Tuple.Create("1", "1"),
            Tuple.Create("2","999")
        };

        public Task AddToRoleAsync(AppUser user, string roleName)
        {
            var role = Roles.FirstOrDefault(x => x.Name == roleName);
            if (role == null) { throw new InvalidOperationException(); }

            var userRoleMap = UserRoleMap.FirstOrDefault(x => x.Item1 == user.Id && x.Item2 == role.Id);
            if (userRoleMap == null)
            {
                UserRoleMap.Add(Tuple.Create(user.Id, role.Id));
            }

            return Task.Delay(0);
        }

        public Task CreateAsync(AppUser user)
        {
            Users.Add(user);
            return Task.Delay(0);
        }

        public Task CreateAsync(AppRole role)
        {
            Roles.Add(role);
            return Task.Delay(0);
        }

        public Task DeleteAsync(AppUser user)
        {
            Users.Remove(Users.First(x => x.Id == user.Id));
            return Task.Delay(0);
        }

        public Task DeleteAsync(AppRole role)
        {
            Roles.Remove(Roles.First(x => x.Id == role.Id));
            return Task.Delay(0);
        }

        public void Dispose()
        {
        }

        public Task<AppUser> FindByIdAsync(string userId)
        {
            return Task.FromResult(Users.FirstOrDefault(u => u.Id == userId));
        }

        public Task<AppUser> FindByNameAsync(string userName)
        {
            return Task.FromResult(Users.FirstOrDefault(u => u.UserName == userName));
        }

        public Task<string> GetPasswordHashAsync(AppUser user)
        {
            return Task.FromResult(new PasswordHasher().HashPassword(user.Password));
        }

        public Task<IList<string>> GetRolesAsync(AppUser user)
        {
            IList<string> roleNames = UserRoleMap.Where(x => x.Item1 == user.Id)
                .Select(x => x.Item2)
                .Select(x => Roles.First(y => y.Id == x))
                .Select(x => x.Name)
                .ToList();
            return Task.FromResult(roleNames);
        }

        public Task<bool> HasPasswordAsync(AppUser user)
        {
            return Task.FromResult(user.Password != null);
        }

        public async Task<bool> IsInRoleAsync(AppUser user, string roleName)
        {
            var roles = await this.GetRolesAsync(user);
            return roles.FirstOrDefault(x => x.ToUpper() == roleName.ToUpper()) != null;
        }

        public Task RemoveFromRoleAsync(AppUser user, string roleName)
        {
            var role = Roles.FirstOrDefault(x => x.Name == roleName);
            if (role == null) { return Task.FromResult(default(object)); }
            var userRoleMap = UserRoleMap.FirstOrDefault(x => x.Item1 == user.Id && x.Item2 == role.Id);
            if (UserRoleMap != null)
            {
                UserRoleMap.Remove(userRoleMap);
            }
            return Task.Delay(0);
        }

        public Task SetPasswordHashAsync(AppUser user, string passwordHash)
        {
            user.Password = passwordHash;
            return Task.Delay(0);
        }

        public async Task UpdateAsync(AppUser user)
        {
            var target = await this.FindByIdAsync(user.Id);
            if (target == null) { return; }
            target.UserName = user.UserName;
            target.Password = user.Password;
            return;
;        }

        public Task UpdateAsync(AppRole role)
        {
            var t = Roles.FirstOrDefault(r => r.Id == role.Id);
            if (t == null) { return Task.FromResult(default(object)); }
            t.Name = role.Name;
            return Task.FromResult(default(object));
        }

        Task<AppRole> IRoleStore<AppRole, string>.FindByIdAsync(string roleId)
        {
            return Task.FromResult(Roles.FirstOrDefault(u => u.Id == roleId));
        }

        Task<AppRole> IRoleStore<AppRole, string>.FindByNameAsync(string roleName)
        {
            return Task.FromResult(Roles.FirstOrDefault(u => u.Name == roleName));
        }
    }
}