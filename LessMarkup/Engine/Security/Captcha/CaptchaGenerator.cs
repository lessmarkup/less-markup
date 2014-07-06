/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.IO;
using System.Web.Mvc;
using System.Web.UI;
using LessMarkup.Interfaces.System;
using DependencyResolver = LessMarkup.Interfaces.DependencyResolver;

namespace LessMarkup.Engine.Security.Captcha
{
    public static class CaptchaGenerator
    {
        public static MvcHtmlString GenerateCaptcha(this HtmlHelper helper)
        {
            var configurationSettings = DependencyResolver.Resolve<IEngineConfiguration>();

            var publicKey = configurationSettings.RecaptchaPublicKey;
            var privateKey = configurationSettings.RecaptchaPrivateKey;

            if (string.IsNullOrEmpty(publicKey) || string.IsNullOrEmpty(privateKey))
            {
                return new MvcHtmlString("");
            }

            using (var captchaControl = new Recaptcha.RecaptchaControl
                                            {
                                                ID = "recaptcha",
                                                PublicKey = publicKey,
                                                PrivateKey = privateKey,
                                                Theme = "clean"
                                            })
            {
                using (var htmlWriter = new HtmlTextWriter(new StringWriter()))
                {
                    captchaControl.RenderControl(htmlWriter);
                    return new MvcHtmlString(htmlWriter.InnerWriter.ToString());
                }
            }
        }
    }
}