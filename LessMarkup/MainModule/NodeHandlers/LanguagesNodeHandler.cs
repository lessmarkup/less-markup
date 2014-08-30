using System.Web.Mvc;
using LessMarkup.Engine.Language;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.Structure;
using LessMarkup.MainModule.Model;
using LessMarkup.UserInterface;
using LessMarkup.UserInterface.NodeHandlers.Common;
using LessMarkup.DataFramework;
using DependencyResolver = LessMarkup.Interfaces.DependencyResolver;

namespace LessMarkup.MainModule.NodeHandlers
{
    [ConfigurationHandler(MainModuleTextIds.Languages)]
    public class LanguagesNodeHandler : RecordListLinkNodeHandler<LanguageModel>
    {
        public LanguagesNodeHandler(IDomainModelProvider domainModelProvider, IDataCache dataCache, ICurrentUser currentUser) : base(domainModelProvider, dataCache, currentUser)
        {
            AddCellLink<TranslationsNodeHandler>(UserInterfaceTextIds.Translations, "translations");
            AddRecordLink(MainModuleTextIds.Export, "export/{Id}", true);
        }

        [RecordAction(MainModuleTextIds.ResetLanguage)]
        public object Reset(long recordId)
        {
            var model = DependencyResolver.Resolve<LanguageModel>();
            model.Id = recordId;
            model.Reset();
            return ReturnMessageResult(LanguageHelper.GetText(Constants.ModuleType.MainModule, MainModuleTextIds.LanguageWasReset));
        }

        public object AddMissing(long recordId)
        {
            var model = DependencyResolver.Resolve<LanguageModel>();
            model.Id = recordId;
            model.AddMissing();
            return ReturnMessageResult(LanguageHelper.GetText(Constants.ModuleType.MainModule, MainModuleTextIds.AddedMissingTranslations));
        }

        [RecordAction(MainModuleTextIds.Import, CreateType = typeof(ImportLanguageModel))]
        public object Import(long recordId, ImportLanguageModel newObject)
        {
            var model = DependencyResolver.Resolve<LanguageModel>();
            model.Id = recordId;
            model.Import(newObject.ImportFile);
            return null;
        }

        [RecordAction(MainModuleTextIds.SetDefault, Visible = "!IsDefault")]
        public object SetDefault(long recordId)
        {
            var model = DependencyResolver.Resolve<LanguageModel>();
            model.Id = recordId;
            model.SetDefault();
            return ReturnResetResult();
        }

        protected override ActionResult CreateResult(string path)
        {
            if (path != null && path.StartsWith("export"))
            {
                var parts = path.Split(new[] {'/'});
                long languageId;
                if (parts.Length == 2 && long.TryParse(parts[1], out languageId))
                {
                    var model = DependencyResolver.Resolve<LanguageModel>();
                    model.Id = languageId;
                    return model.Export();
                }
            }

            return null;
        }
    }
}
