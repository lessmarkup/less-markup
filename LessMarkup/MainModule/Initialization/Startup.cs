﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using LessMarkup.MainModule.Initialization;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(Startup))]
namespace LessMarkup.MainModule.Initialization
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
        }
    }
}