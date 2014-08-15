using System.Web.Mvc;
using LessMarkup.MainModule.Model;

namespace LessMarkup.MainModule.Controllers
{
    public class AccountController : Controller
    {
        public ActionResult Validate(string secret)
        {
            var model = Interfaces.DependencyResolver.Resolve<UserValidateModel>();
            model.ValidateSecret(secret, Url);
            return View(model);
        }

        public ActionResult Approve(string secret)
        {
            var model = Interfaces.DependencyResolver.Resolve<ValidateApprovalModel>();
            model.ValidateSecret(secret);
            return View(model);
        }
    }
}
