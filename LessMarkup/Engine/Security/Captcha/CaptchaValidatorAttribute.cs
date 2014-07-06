/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Web.Mvc;
using LessMarkup.Interfaces.System;

namespace LessMarkup.Engine.Security.Captcha
{
    public class CaptchaValidatorAttribute : ActionFilterAttribute
    {
        private const string ChallengeFieldKey = "recaptcha_challenge_field";
        private const string ResponseFieldKey = "recaptcha_response_field";

        public IEngineConfiguration EngineConfiguration { get; set; }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var publicKey = EngineConfiguration.RecaptchaPublicKey;
            var privateKey = EngineConfiguration.RecaptchaPrivateKey;

            if (string.IsNullOrEmpty(publicKey) || string.IsNullOrEmpty(privateKey))
            {
                filterContext.ActionParameters["captchaValid"] = true;
                base.OnActionExecuting(filterContext);
                return;
            }

            var challengeValue = filterContext.HttpContext.Request.Form[ChallengeFieldKey];
            var responseValue = filterContext.HttpContext.Request.Form[ResponseFieldKey];

            var validator = new Recaptcha.RecaptchaValidator
            {
                PrivateKey = privateKey,
                Challenge = challengeValue,
                Response = responseValue,
                RemoteIP = filterContext.HttpContext.Request.UserHostAddress
            };

            var response = validator.Validate();

            filterContext.ActionParameters["captchaValid"] = response.IsValid;
            filterContext.ActionParameters["captchaError"] = response.ErrorMessage;

            base.OnActionExecuting(filterContext);
        }
    }
}