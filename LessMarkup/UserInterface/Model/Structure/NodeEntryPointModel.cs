/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LessMarkup.DataFramework;
using LessMarkup.DataObjects.Common;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.Structure;
using LessMarkup.Interfaces.System;
using LessMarkup.UserInterface.Model.User;
using Newtonsoft.Json;
using DependencyResolver = LessMarkup.Interfaces.DependencyResolver;

namespace LessMarkup.UserInterface.Model.Structure
{
    public class NodeEntryPointModel
    {
        private readonly IDataCache _dataCache;
        private readonly ICurrentUser _currentUser;
        private readonly IEngineConfiguration _engineConfiguration;
        private readonly IDomainModelProvider _domainModelProvider;
        private LoadNodeViewModel ViewData { get; set; }

        public string Title { get; set; }
        public string Body { get; set; }
        public string LogoImageUrl { get; set; }
        public string InitialData { get; set; }
        public string ScriptInitialData { get { return string.Format("<script>window.viewInitialData = {0};</script>", InitialData); } }
        public ActionResult Result { get; set; }
        public bool NoScript { get; set; }
        public bool OnlyBody { get; set; }

        public NodeEntryPointModel(IDataCache dataCache, ICurrentUser currentUser, IEngineConfiguration engineConfiguration, IDomainModelProvider domainModelProvider)
        {
            _dataCache = dataCache;
            _currentUser = currentUser;
            _engineConfiguration = engineConfiguration;
            _domainModelProvider = domainModelProvider;
        }

        private void CheckBrowser(System.Web.Mvc.Controller controller)
        {
            var browser = controller.Request.Browser;

            if (browser == null || browser.EcmaScriptVersion == null || browser.EcmaScriptVersion.Major < 1 || browser.Crawler)
            {
                NoScript = true;
                return;
            }

            if (browser.Browser != null)
            {
                switch (browser.Browser)
                {
                    case "InternetExplorer":
                        if (browser.MajorVersion < 9)
                        {
                            NoScript = true;
                        }
                        break;
                    case "Chrome":
                        break;
                    case "Firefox":
                        break;
                }
            }
        }

        public bool Initialize(string path, System.Web.Mvc.Controller controller)
        {
            CheckBrowser(controller);

            if (!NoScript)
            {
                var noScriptCookie = controller.Request.Cookies.Get("noscript");
                if (noScriptCookie != null && noScriptCookie.Value == "true")
                {
                    NoScript = true;
                }
            }

            var queryString = controller.Request.RawUrl;
            if (queryString != null && (queryString.EndsWith("?noscript") || queryString == "noscript"))
            {
                NoScript = true;
                OnlyBody = true;
            }

            string nodeLoadError = null;

            ViewData = DependencyResolver.Resolve<LoadNodeViewModel>();
            try
            {
                if (!ViewData.Initialize(path, null, controller, true, true))
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                this.LogException(e);
                nodeLoadError = e.Message;
            }

            if (ViewData.Result != null)
            {
                Result = ViewData.Result;
                return true;
            }

            Body = _dataCache.Get<IResourceCache>().ReadText("~/Views/Body.html");

            var nodeCache = _dataCache.Get<INodeCache>();
            var recordModelCache = _dataCache.Get<IRecordModelCache>();

            var rootNode = nodeCache.RootNode;
            
            Title = rootNode.Title;

            var initialValues = new Dictionary<string, object>();

            InitializeSiteProperties(controller, initialValues);

            if (NoScript)
            {
                var pos = Body.IndexOf(Constants.Engine.NoScriptBlock, StringComparison.Ordinal);
                if (pos <= 0)
                {
                    return true;
                }

                var viewPath = LoadNodeViewModel.GetViewPath(ViewData.NodeHandler.ViewType) + "NoScript";
                var resourceCache = _dataCache.Get<IResourceCache>(_dataCache.Get<ILanguageCache>().CurrentLanguageId);
                var template = resourceCache.ReadText(viewPath + ".html") ?? LoadNodeViewModel.GetViewContents(viewPath + ".cshtml", ViewData, controller);

                Body = OnlyBody ? template : Body.Remove(pos, Constants.Engine.NoScriptBlock.Length).Insert(pos, template ?? "");

                return true;
            }

            initialValues["rootPath"] = rootNode.FullPath;
            initialValues["path"] = path ?? "";
            initialValues["showConfiguration"] = _currentUser.IsAdministrator;
            initialValues["configurationPath"] = "/" + Constants.NodePath.Configuration;
            initialValues["profilePath"] = "/" + Constants.NodePath.Profile;
            initialValues["forgotPasswordPath"] = "/" + Constants.NodePath.ForgotPassword;
            initialValues["loggedIn"] = _currentUser.UserId.HasValue;
            initialValues["userNotVerified"] = !_currentUser.IsApproved || !_currentUser.EmailConfirmed;
            initialValues["userName"] = _currentUser.Name ?? "";
            initialValues["viewData"] = ViewData;
            initialValues["nodeLoadError"] = nodeLoadError;
            initialValues["recaptchaPublicKey"] = _engineConfiguration.RecaptchaPublicKey;
            initialValues["loginModelId"] = recordModelCache.GetDefinition<LoginModel>().Id;

            InitialData = JsonConvert.SerializeObject(initialValues);

            return true;
        }

        private void InitializeSiteProperties(System.Web.Mvc.Controller controller, Dictionary<string, object> initialValues)
        {
            var changesCache = _dataCache.Get<IChangesCache>();
            var versionId = changesCache.LastChangeId;
            initialValues["versionId"] = versionId;

            var siteConfiguration = _dataCache.Get<ISiteConfiguration>();
            var adminLoginPage = siteConfiguration.AdminLoginPage;
            if (string.IsNullOrWhiteSpace(adminLoginPage))
            {
                adminLoginPage = _engineConfiguration.AdminLoginPage;
            }
            initialValues["hasLogin"] = siteConfiguration.HasUsers || string.IsNullOrWhiteSpace(adminLoginPage);
            initialValues["hasSearch"] = siteConfiguration.HasSearch;

            var notificationsModel = DependencyResolver.Resolve<UserInteraceElementsModel>();
            notificationsModel.Handle(initialValues, controller, versionId);

            if (siteConfiguration.UseLanguages)
            {
                var languageCache = _dataCache.Get<ILanguageCache>();
                var languageId = languageCache.CurrentLanguageId;

                var languages = languageCache.Languages.Select(l => new
                {
                    Id = l.LanguageId,
                    l.Name,
                    l.ShortName,
                    Url = string.Format("/language/{0}", l.LanguageId),
                    ImageUrl = l.IconId.HasValue ? ImageHelper.ImageUrl(l.IconId.Value) : null,
                    Selected = l.LanguageId == languageId
                });

                initialValues["languages"] = languages;
            }

            using (var domainModel = _domainModelProvider.Create())
            {
                initialValues["smiles"] = domainModel.Query().From<Smile>().ToList<Smile>().Select(s => new {s.Id, s.Code}).ToList();
                initialValues["smilesBase"] = "/Image/Smile/";
            }

            initialValues["rootTitle"] = siteConfiguration.SiteName;
            initialValues["maximumFileSize"] = siteConfiguration.MaximumFileSize;
            initialValues["useGoogleAnalytics"] = !string.IsNullOrWhiteSpace(siteConfiguration.GoogleAnalyticsResource);
        }

        public ActionResult CreateResult(System.Web.Mvc.Controller controller)
        {
            if (Result != null)
            {
                return Result;
            }

            var result = new ViewResult();
            controller.ViewData.Model = this;
            result.ViewName = OnlyBody ? "~/Views/BodyNoScript.cshtml" : "~/Views/EntryPoint.cshtml";
            result.ViewData = controller.ViewData;
            result.TempData = controller.TempData;
            result.MasterName = null;
            result.ViewEngineCollection = controller.ViewEngineCollection;

            if (OnlyBody)
            {
                var cookie = new HttpCookie("noscript");
                cookie.Value = "true";
                controller.Response.Cookies.Add(cookie);
            }

            return result;
        }
    }
}
