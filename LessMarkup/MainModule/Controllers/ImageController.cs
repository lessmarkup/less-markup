using System.Web.Mvc;
using LessMarkup.MainModule.Model;

namespace LessMarkup.MainModule.Controllers
{
    public class ImageController : Controller
    {
        public ActionResult Get(long id)
        {
            var model = Interfaces.DependencyResolver.Resolve<ImageModel>();
            return model.Get(id);
        }

        public ActionResult Thumbnail(long id)
        {
            var model = Interfaces.DependencyResolver.Resolve<ImageModel>();
            return model.Thumbnail(id);
        }

        public ActionResult Smile(long id)
        {
            var model = Interfaces.DependencyResolver.Resolve<ImageModel>();
            return model.Smile(id);
        }
    }
}
