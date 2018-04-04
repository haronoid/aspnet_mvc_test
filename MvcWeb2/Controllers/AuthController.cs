using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace MvcWeb2.Controllers
{
    [AllowAnonymous]
    public class AuthController : Controller
    {
        // GET: Auth
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }

        //
        // POST: /Auth/Login
        [HttpPost]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (!this.ModelState.IsValid)
            {
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }

            // 認証
            var userManager = new UserManager<AppUser>(new AppUserStore());
            var user = await userManager.FindAsync(model.UserName, model.Password);
            if (user == null)
            {
                // 認証失敗したらエラーメッセージを設定してログイン画面を表示する
                this.ModelState.AddModelError("", "ユーザ名かパスワードが違います");
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }

            // クレームベースのIDを作って
            var identify = await userManager.CreateIdentityAsync(
                user,
                DefaultAuthenticationTypes.ApplicationCookie);

            // 認証情報を設定
            var authentication = this.HttpContext.GetOwinContext().Authentication;
            authentication.SignIn(identify);

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