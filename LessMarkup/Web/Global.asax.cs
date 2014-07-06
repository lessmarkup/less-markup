using LessMarkup.MainModule;
using XSockets.Core.Common.Socket;

namespace LessMarkup.Web
{
    public class MvcApplication : CoreApplication
    {
        private IXSocketServerContainer _serverContainer;

        protected void Application_Start()
        {
            _serverContainer = XSockets.Plugin.Framework.Composable.GetExport<IXSocketServerContainer>();
            _serverContainer.StartServers();
        }
    }
}
