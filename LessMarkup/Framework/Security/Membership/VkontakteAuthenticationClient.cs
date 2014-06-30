/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Web;
using System.Web.Script.Serialization;
using DotNetOpenAuth.AspNet;

namespace LessMarkup.Framework.Security.Membership
{
    public class VkontakteAuthenticationClient : IAuthenticationClient
    {
        private string _redirectUri;
        private readonly string _appId;
        private readonly string _appSecret;

        public VkontakteAuthenticationClient(string appId, string appSecret)
        {
            _appId = appId;
            _appSecret = appSecret;
        }

        public string ProviderName { get { return "vkontakte"; } }

        public void RequestAuthentication(HttpContextBase context, Uri returnUrl)
        {
            _redirectUri = context.Server.UrlEncode(returnUrl.ToString());
            var address = String.Format("https://oauth.vk.com/authorize?client_id={0}&redirect_uri={1}&response_type=code", _appId, _redirectUri);
 
            HttpContext.Current.Response.Redirect(address, false);
        }
 
        public AuthenticationResult VerifyAuthentication(HttpContextBase context)
        {
            try
            {
                string code = context.Request["code"];
 
                var address = String.Format("https://oauth.vk.com/access_token?client_id={0}&client_secret={1}&code={2}&redirect_uri={3}", _appId, _appSecret, code, _redirectUri);
 
                var accessToken = DeserializeJson(Load(address));

                string userId = accessToken["user_id"].ToString();
 
                address = String.Format("https://api.vk.com/method/users.get?uids={0}&fields=photo_50", userId);
 
                var usersData = DeserializeJson(Load(address));
                var userData = usersData["response"][0];

                var externalData = new Dictionary<string, string>();
                foreach (var k in userData)
                {
                    externalData.Add(k.Key, k.Value.ToString());
                }

                string firstName = userData["first_name"];
                string lastName = userData["last_name"];
                var fullName = firstName + " " + lastName;
 
                return new AuthenticationResult(true, ProviderName, userId, fullName, externalData);
            }
            catch (Exception ex)
            {
                return new AuthenticationResult(ex);
            }
        }
 
        public static string Load(string address)
        {
            var request = WebRequest.Create(address);
            using (var response = request.GetResponse())
            {
                using (var stream = response.GetResponseStream())
                {
                    if (stream == null)
                    {
                        return string.Empty;
                    }

                    using (var reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
        }
 
        public static dynamic DeserializeJson(string input)
        {
            var serializer = new JavaScriptSerializer();
            return serializer.Deserialize<dynamic>(input);
        }
    }
}
