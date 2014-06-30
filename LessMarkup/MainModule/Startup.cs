/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using LessMarkup.MainModule;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(Startup))]
namespace LessMarkup.MainModule
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            CoreApplication.InitializeDependencyResolver();
            app.MapSignalR("/hubs", new HubConfiguration { });
        }
    }
}
