using System.Web.Mvc;
using LessMarkup.MainModule.Model;

namespace LessMarkup.MainModule.Controllers
{
    public class FileController : Controller
    {
        public ActionResult Get(string id)
        {
            var model = Interfaces.DependencyResolver.Resolve<FileModel>();
            return model.GetFile(id);
        }
    }
}
