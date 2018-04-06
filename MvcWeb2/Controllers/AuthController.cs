using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace MvcWeb2.Controllers
{
    [Authorize]
    public class AuthController : Controller
    {
        private AppSignInManager _appSignInManager;
        private AppUserManager _appUserManager;

        public AppSignInManager SignInManager
        {
            get
            {
                // StartUp.csでCreatePerOwinContext登録したオブジェクトを取り出す。
                return _appSignInManager ?? HttpContext.GetOwinContext().Get<AppSignInManager>();
            }
            private set
            {
                _appSignInManager = value;
            }
        }

        public AppUserManager UserManager
        {
            get
            {
                // StartUp.csでCreatePerOwinContext登録したオブジェクトを取り出す。
                return _appUserManager ?? HttpContext.GetOwinContext().Get<AppUserManager>();
            }
            private set
            {
                _appUserManager = value;
            }
        }

        // GET: Auth/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }

        //
        // POST: /Auth/Login
        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (!this.ModelState.IsValid)
            {
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }

            // 認証
            //var userManager = new UserManager<AppUser>(new AppUserStore());
            //var user = await userManager.FindAsync(model.UserName, model.Password);
            //if (user == null)
            //{
            //    // 認証失敗したらエラーメッセージを設定してログイン画面を表示する
            //    this.ModelState.AddModelError("", "ユーザ名かパスワードが違います");
            //    ViewBag.ReturnUrl = returnUrl;
            //    return View(model);
            //}

            //// クレームベースのIDを作って
            //var identify = await userManager.CreateIdentityAsync(
            //    user,
            //    DefaultAuthenticationTypes.ApplicationCookie);

            //// 認証情報を設定
            //var authentication = this.HttpContext.GetOwinContext().Authentication;
            //authentication.SignIn(identify);


            var user = await UserManager.FindAsync(model.UserName, model.Password);
            if (user == null) {
                this.ModelState.AddModelError("", "ユーザ名かパスワードが違います");
                return View(model);
            }

            await SignInManager.SignInAsync(user, false, false);

            // 元のページへリダイレクト
            return Redirect(returnUrl);
        }
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "ユーザー名を入れてください")]
        [Display(Name = "ユーザー名")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "パスワードを入れてください")]
        [Display(Name = "パスワード")]
        public string Password { get; set; }
    }
}