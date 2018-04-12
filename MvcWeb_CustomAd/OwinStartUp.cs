using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.AspNet.Identity;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
//using Microsoft.Owin.Security.OpenIdConnect;
using System.DirectoryServices.AccountManagement;
using System.Security.Claims;

[assembly: OwinStartup(typeof(MvcWeb.OwinStartUp))]
namespace MvcWeb
{
    
    public class OwinStartUp
    {
        public void Configuration(IAppBuilder app)
        {
            app.CreatePerOwinContext(() => new PrincipalContext(ContextType.Domain));
            app.CreatePerOwinContext<AppUserMananger>(AppUserMananger.Create);
            app.CreatePerOwinContext<AppSignInManager>(AppSignInManager.Create);
            
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions()
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                LoginPath = new PathString("/Account/Login"),
                Provider = new CookieAuthenticationProvider
                {
                    OnValidateIdentity = SecurityStampValidator.OnValidateIdentity<AppUserMananger, AppUser>(
                        validateInterval: TimeSpan.FromMinutes(30),
                        regenerateIdentity: (manager,user) => user.GenerateUserIdentityAsync(manager))
                }
            });

        }
    }

    public class AppUser : IUser<string>
    {
        private UserPrincipal _adUser;

        public AppUser(UserPrincipal adUser)
        {
            this._adUser = adUser;
        }

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<AppUser> manager)
        {
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            return userIdentity;
        }

        #region IUser<string> Members

        public string Id => _adUser.SamAccountName;

        public string UserName
        {
            get => _adUser.SamAccountName;
            set { throw new System.NotImplementedException(); }
        }
        #endregion
    }

    public class AppUserStore :
        IUserStore<AppUser>,
        IUserStore<AppUser, string>
    {
        private readonly PrincipalContext _context;
        private AppUserStore(PrincipalContext context)
        {
            _context = context;
        }

        // context.Get<PrincipalContext>()でADのcontextを取得
        public static AppUserStore Create(IdentityFactoryOptions<AppUserStore> options, IOwinContext context)
        {
            //var principalContext = new PrincipalContext(ContextType.Domain);
            var principalContext = context.Get<PrincipalContext>();
            return new AppUserStore(principalContext);
        }

        #region IUserStore<MyUser, string> Members

        public Task CreateAsync(AppUser user)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(AppUser user)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        // UserPrincipal.FindByIdentityにより、検索する
        public Task<AppUser> FindByIdAsync(string userId)
        {
            var user = UserPrincipal.FindByIdentity(_context, userId);
            return Task.FromResult<AppUser>(new AppUser(user));
        }

        public Task<AppUser> FindByNameAsync(string userName)
        {
            var user = UserPrincipal.FindByIdentity(_context, userName);
            return Task.FromResult<AppUser>(new AppUser(user));
        }

        public Task UpdateAsync(AppUser user)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    public class AppUserMananger : UserManager<AppUser>
    {
        private readonly PrincipalContext _context;

        private AppUserMananger(IUserStore<AppUser> store, PrincipalContext context) : base(store)
        {
            _context = context;
        }
        
        public static AppUserMananger Create(IdentityFactoryOptions<AppUserMananger> options, IOwinContext context)
        {
            var userStore = context.Get<AppUserStore>();
            var principalContext = context.Get<PrincipalContext>();
            return new AppUserMananger(userStore, principalContext);
        }

        public override async Task<bool> CheckPasswordAsync(AppUser user, string password)
        {
            return await Task.FromResult(_context.ValidateCredentials(user.UserName, password, ContextOptions.Negotiate));
        }
    }

    public class AppSignInManager : SignInManager<AppUser, string>
    {
        UserManager<AppUser, string> _manager;

        private AppSignInManager(UserManager<AppUser, string> userManager, IAuthenticationManager authenticationManager) 
            : base(userManager, authenticationManager)
        {
            _manager = UserManager;
        }

        public static AppSignInManager Create(IdentityFactoryOptions<AppSignInManager> options, IOwinContext context)
        {
            AppUserMananger manager = context.GetUserManager<AppUserMananger>();
            return new AppSignInManager(manager, context.Authentication);
        }

        public override Task<ClaimsIdentity> CreateUserIdentityAsync(AppUser user)
        {
            return user.GenerateUserIdentityAsync((AppUserMananger)UserManager);
        }

        public override async Task<SignInStatus> PasswordSignInAsync(string userName, string password, bool isPersistent, bool shouldLockout)
        {
            var result = SignInStatus.Failure;

            try
            {
                var user = await this.UserManager.FindAsync(userName, password);
                if (user != null)
                {
                    await this.SignInAsync(user, isPersistent, true);
                    result = SignInStatus.Success;
                }
            } catch
            {
                result = SignInStatus.Failure;
            }

            return result;
        }
    }
}