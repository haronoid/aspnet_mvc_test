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
using MvcWeb2.Models;

[assembly: OwinStartup(typeof(MvcWeb2.OwinStartUp))]
namespace MvcWeb2
{
    public class OwinStartUp
    {
        public void Configuration(IAppBuilder app)
        {
            // 後から「HttpContext.GetOwinContext().Get」で取り出して使える。
            app.CreatePerOwinContext(() => new AppUserStore());
            app.CreatePerOwinContext<AppUserManager>(AppUserManager.Create);
            app.CreatePerOwinContext<AppSignInManager>(AppSignInManager.Create);

            app.CreatePerOwinContext<AppRoleManager>(AppRoleManager.Create);

            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                LoginPath = new PathString("/Auth/Login")
            });
        }
    }

    public class WebUser : IUser<string>
    {
        private string _id = Guid.NewGuid().ToString();
        private string _userName;
        private string _password;
        private string _hashedPassword;

        public string Id { get => _id; set => _id = value; }
        public string UserName { get => _userName; set => _userName = value; }
        public string Password { get => _password; set => _password = value; }
        public string HashedPassword { get => _hashedPassword; set => _hashedPassword = value; }
    }

    public class AppUserManager : UserManager<WebUser>
    {
        private IUserStore<WebUser> _store;
        private AppUserManager(IUserStore<WebUser> store)
            : base(store)
        {
            _store = store;
        }

        public static AppUserManager Create(IdentityFactoryOptions<AppUserManager> options, IOwinContext context)
        {
            var store = context.Get<AppUserStore>();
            return new AppUserManager(store);
        }

        public override async Task<WebUser> FindAsync(string userName, string password)
        {
            var user = await _store.FindByNameAsync(userName);
            if (user != null)
            {
                var result = new PasswordHasher().VerifyHashedPassword(user.HashedPassword, password);

            }

            return user;

        }
    }

    public class AppSignInManager : SignInManager<WebUser, string>
    {
        private AppSignInManager(UserManager<WebUser, string> userManager, IAuthenticationManager authenticationManager)
            : base(userManager, authenticationManager)
        { }

        public static AppSignInManager Create(IdentityFactoryOptions<AppSignInManager> options, IOwinContext context)
        {
            var manager = context.Get<AppUserManager>();
            return new AppSignInManager(manager, context.Authentication);
        }
    }

    public class AppRole : IRole<string>
    {
        private string _id = Guid.NewGuid().ToString();
        private string _name;

        public string Id { get => _id; set => _id = value; }
        public string Name { get => _name; set => _name = value; }
    }

    public class AppRoleManager : RoleManager<AppRole>
    {
        private AppRoleManager(IRoleStore<AppRole, string> store) 
            : base(store)
        { }

        public static AppRoleManager Create(IdentityFactoryOptions<AppRoleManager> options, IOwinContext context)
        {
            var store = context.Get<AppUserStore>();
            return new AppRoleManager(store);
        }
    }

    public class AppUserStore : 
        IUserStore<WebUser>, 
        IUserStore<WebUser,string>,
        IUserPasswordStore<WebUser,string>,
        IUserRoleStore<WebUser, string>,
        IRoleStore<AppRole, string>
    {

        private LocalDbEntities _dbContext;

        public AppUserStore()
        {
            _dbContext = new LocalDbEntities();

            if (_dbContext.AppUser.Count() == 0)
            {
                _dbContext.AppUser.Add(new Models.AppUser() {
                    Id = Guid.NewGuid(),
                    UserName = "user1",
                    Password = new PasswordHasher().HashPassword("abc")
                });
                _dbContext.AppUser.Add(new Models.AppUser()
                {
                    Id = Guid.NewGuid(),
                    UserName = "user2",
                    Password = new PasswordHasher().HashPassword("def")
                });
                _dbContext.SaveChanges();
            }

        }
        
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

        public Task AddToRoleAsync(WebUser user, string roleName)
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

        public Task CreateAsync(WebUser user)
        {

            Models.AppUser newUser = new Models.AppUser()
            {
                Id = Guid.NewGuid(),
                UserName = user.UserName,
                Password = user.HashedPassword
            };

            _dbContext.AppUser.Add(newUser);

            return Task.Delay(0);
        }

        public Task CreateAsync(AppRole role)
        {
            Roles.Add(role);
            return Task.Delay(0);
        }

        public Task DeleteAsync(WebUser user)
        {
            _dbContext.AppUser.Remove(_dbContext.AppUser.First(x => x.UserName == user.UserName));
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

        public Task<WebUser> FindByIdAsync(string userId)
        {
            WebUser ret = null;
            var user = _dbContext.AppUser.FirstOrDefault(u => u.UserName == userId);
            if (user != null)
            {
                ret = new WebUser()
                {
                    Id = user.Id.ToString(),
                    UserName = user.UserName,
                    HashedPassword = user.Password
                };
            }

            return Task.FromResult(ret);
        }

        public Task<WebUser> FindByNameAsync(string userName)
        {
            WebUser ret = null;
            var user = _dbContext.AppUser.FirstOrDefault(u => u.UserName == userName);
            if (user != null)
            {
                ret = new WebUser()
                {
                    Id = user.Id.ToString(),
                    UserName = user.UserName,
                    HashedPassword = user.Password
                };
            }
            return Task.FromResult(ret);
        }

        public Task<string> GetPasswordHashAsync(WebUser user)
        {
            return Task.FromResult(new PasswordHasher().HashPassword(user.Password));
        }

        public Task<IList<string>> GetRolesAsync(WebUser user)
        {
            IList<string> roleNames = UserRoleMap.Where(x => x.Item1 == user.Id)
                .Select(x => x.Item2)
                .Select(x => Roles.First(y => y.Id == x))
                .Select(x => x.Name)
                .ToList();
            return Task.FromResult(roleNames);
        }

        public Task<bool> HasPasswordAsync(WebUser user)
        {
            return Task.FromResult(user.Password != null);
        }

        public async Task<bool> IsInRoleAsync(WebUser user, string roleName)
        {
            var roles = await this.GetRolesAsync(user);
            return roles.FirstOrDefault(x => x.ToUpper() == roleName.ToUpper()) != null;
        }

        public Task RemoveFromRoleAsync(WebUser user, string roleName)
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

        public Task SetPasswordHashAsync(WebUser user, string passwordHash)
        {
            user.Password = passwordHash;
            return Task.Delay(0);
        }

        public async Task UpdateAsync(WebUser user)
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