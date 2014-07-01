using System.Text;
using System.Web.Mvc;
using LessMarkup.Interfaces.Module;
using LessMarkup.Interfaces.Structure;

namespace LessMarkup.Framework.Helpers
{
    public static class LoadHelper
    {
        public static MvcHtmlString RenderNodeHandlerScripts(this HtmlHelper htmlHelper)
        {
            var builder = new StringBuilder();
            var moduleIntegration = Interfaces.DependencyResolver.Resolve<IModuleIntegration>();
            foreach (var nodeHandlerTd in moduleIntegration.GetNodeHandlers())
            {
                var nodeHandler = (INodeHandler) Interfaces.DependencyResolver.Resolve(moduleIntegration.GetNodeHandler(nodeHandlerTd).Item1);
                foreach (var path in nodeHandler.Scripts)
                {
                    builder.AppendFormat("<script src=\"{0}\"></script>", path);
                }
            }

            return new MvcHtmlString(builder.ToString());
        }

        public static MvcHtmlString RenderNodeHandlerStylesheets(this HtmlHelper htmlHelper)
        {
            var builder = new StringBuilder();
            var moduleIntegration = Interfaces.DependencyResolver.Resolve<IModuleIntegration>();
            foreach (var nodeHandlerTd in moduleIntegration.GetNodeHandlers())
            {
                var nodeHandler = (INodeHandler)Interfaces.DependencyResolver.Resolve(moduleIntegration.GetNodeHandler(nodeHandlerTd).Item1);
                foreach (var path in nodeHandler.Stylesheets)
                {
                    builder.AppendFormat("<link rel=\"stylesheet\" href=\"{0}\"></script>", path);
                }
            }

            return new MvcHtmlString(builder.ToString());
        }
    }
}
