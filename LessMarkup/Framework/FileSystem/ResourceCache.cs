/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using LessMarkup.DataFramework;
using LessMarkup.DataObjects.Common;
using LessMarkup.Framework.Build.View;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Module;
using LessMarkup.Interfaces.System;

namespace LessMarkup.Framework.FileSystem
{
    public class ResourceCache : ICacheHandler
    {
        private readonly IDomainModelProvider _domainModelProvider;
        private readonly ISiteMapper _siteMapper;
        private readonly ISpecialFolder _specialFolder;
        private readonly IModuleProvider _moduleProvider;
        private readonly object _loadLock = new object();

        private Assembly _globalAssembly;

        private readonly Dictionary<string, ResourceReference> _resourceReferences = new Dictionary<string, ResourceReference>();
        private readonly Dictionary<string, ViewReference> _viewReferences = new Dictionary<string, ViewReference>();

        public ResourceCache(IDomainModelProvider domainModelProvider, ISiteMapper siteMapper, ISpecialFolder specialFolder, IModuleProvider moduleProvider)
        {
            _domainModelProvider = domainModelProvider;
            _siteMapper = siteMapper;
            _specialFolder = specialFolder;
            _moduleProvider = moduleProvider;
        }

        private string ExtractPath(string path)
        {
            if (!path.StartsWith("~/"))
            {
                return path;
            }
            return path.Substring(2);
        }

        public bool ResourceExists(string path)
        {
            return _resourceReferences.ContainsKey(ExtractPath(path).ToLower());
        }

        private ResourceReference LoadResource(string path)
        {
            ResourceReference reference;
            if (!_resourceReferences.TryGetValue(path.ToLower(), out reference))
            {
                return null;
            }

            if (reference.DataLoaded)
            {
                return reference;
            }

            lock (_loadLock)
            {
                if (reference.DataLoaded)
                {
                    return reference;
                }

                using (var domainModel = _domainModelProvider.Create())
                {
                    var record = domainModel.GetSiteCollection<SiteCustomization>().SingleOrDefault(c => c.SiteCustomizationId == reference.RecordId);
                    if (record == null)
                    {
                        reference.DataLoaded = true;
                        return reference;
                    }

                    reference.Binary = record.Body;
                }
            }

            return reference;
        }

        public Stream ReadResource(string path)
        {
            var reference = LoadResource(ExtractPath(path));

            if (reference == null || reference.Binary == null)
            {
                return null;
            }

            var binaryData = reference.Binary;

            return new MemoryStream(binaryData);
        }

        public string ReadText(string path)
        {
            var reference = LoadResource(ExtractPath(path));

            if (reference == null || reference.Binary == null)
            {
                return null;
            }

            return Encoding.UTF8.GetString(reference.Binary);
        }

        public bool DirectoryExists(string path)
        {
            path = ExtractPath(path);

            if (string.IsNullOrWhiteSpace(path))
            {
                return true;
            }

            var subPath = path.EndsWith("/") ? path : path + "/";
            subPath = subPath.ToLower();

            foreach (var resourcePath in _resourceReferences.Keys.Where(p => p.StartsWith(subPath)))
            {
                var localPath = resourcePath.Remove(0, subPath.Length);
                if (!string.IsNullOrWhiteSpace(localPath))
                {
                    return true;
                }
            }

            return false;
        }

        public List<string> GetDirectories(string path)
        {
            path = ExtractPath(path);

            var ret = new List<string>();

            var subPath = path.EndsWith("/") ? path : path + "/";

            subPath = subPath.ToLower();

            foreach (var resourcePath in _resourceReferences.Keys.Where(p => p.StartsWith(subPath)))
            {
                var localPath = resourcePath.Remove(0, subPath.Length);
                if (!localPath.Contains('/'))
                {
                    continue;
                }

                var directory = localPath.Substring(0, localPath.IndexOf('/'));

                if (!ret.Contains(directory))
                {
                    ret.Add(directory);
                }
            }

            return ret;
        }

        public List<string> GetFiles(string path)
        {
            path = ExtractPath(path);

            var ret = new List<string>();

            var subPath = path.EndsWith("/") ? path : path + "/";

            subPath = subPath.ToLower();

            foreach (var resourcePath in _resourceReferences.Keys.Where(p => p.StartsWith(subPath)))
            {
                var localPath = resourcePath.Remove(0, subPath.Length);
                if (localPath.Contains('/'))
                {
                    continue;
                }

                ret.Add(localPath);
            }

            return ret;
        }

        public Stream ReadScript(string path)
        {
            return ReadResource("Scripts/" + ExtractPath(path));
        }

        public Stream ReadImage(string path)
        {
            return ReadResource("Images/" + ExtractPath(path));
        }

        public Stream ReadCss(string path)
        {
            return ReadResource("Css/" + ExtractPath(path));
        }

        public Type LoadType(string path)
        {
            ViewReference viewReference;
            if (!_viewReferences.TryGetValue(path, out viewReference))
            {
                return null;
            }

            if (viewReference.TypeLoaded)
            {
                return viewReference.Type;
            }

            var assembly = _globalAssembly;

            lock (_loadLock)
            {
                viewReference.TypeLoaded = true;
                viewReference.Type = assembly.GetType(viewReference.ClassName, false);
            }

            return viewReference.Type;
        }

        private void ImportTemplateRecord(string recordId, string path, byte[] resource, SiteCustomizationType type)
        {
            if (resource == null)
            {
                throw new ArgumentOutOfRangeException("resource");
            }

            if (type != SiteCustomizationType.View)
            {
                _resourceReferences[path.ToLower()] = new ResourceReference
                {
                    Binary = resource,
                    DataLoaded = true,
                };
            }
            else
            {
                _viewReferences[path] = new ViewReference
                {
                    ClassName = recordId,
                    IsSiteAssembly = false,
                    Path = path,
                };
            }
        }

        private void ImportMailTemplate(string key, ViewTemplate viewTemplate)
        {
            var className = Constants.MailTemplates.Namespace + "." + key;
            ImportTemplateRecord(className, key, Encoding.UTF8.GetBytes(viewTemplate.Body), SiteCustomizationType.View);
        }

        private void ImportViewTemplate(ViewTemplate viewTemplate)
        {
            string pageNamespace, pageClassName;
            ViewBuilder.ExtractPageClassName(viewTemplate, out pageNamespace, out pageClassName);
            var className = pageNamespace + "." + pageClassName;
            ImportTemplateRecord(className, viewTemplate.Path, Encoding.UTF8.GetBytes(viewTemplate.Body), SiteCustomizationType.View);
        }

        private void ImportContentTemplate(ContentTemplate contentTemplate)
        {
            ImportTemplateRecord(contentTemplate.Name, contentTemplate.Name, contentTemplate.Binary, SiteCustomizationType.Image);
        }

        private void LoadDatabaseResources(long siteId)
        {
            using (var domainModel = _domainModelProvider.Create())
            {
                IQueryable<SiteCustomization> collection = domainModel.GetSiteCollection<SiteCustomization>(siteId);

                foreach (var record in collection)
                {
                    _resourceReferences[record.Path.ToLower()] = new ResourceReference
                    {
                        RecordId = record.SiteCustomizationId,
                    };
                }
            }
        }

        public void Initialize(out DateTime? expirationTime, long? objectId = null)
        {
            if (objectId.HasValue)
            {
                throw new ArgumentOutOfRangeException("objectId");
            }

            _globalAssembly = Assembly.LoadFile(_specialFolder.GeneratedViewAssembly);

            var viewImport = new ViewImport();
            foreach (var module in _moduleProvider.Modules)
            {
                viewImport.ImportModule(module, true);
            }

            foreach (var template in viewImport.ViewTemplates)
            {
                if (template.Key.StartsWith("EmailTemplates."))
                {
                    ImportMailTemplate(template.Key.Substring("EmailTemplates.".Length), template.Value);
                }
                else
                {
                    ImportViewTemplate(template.Value);
                }
            }

            foreach (var resource in viewImport.ContentTemplates)
            {
                ImportContentTemplate(resource.Value);
            }

            var siteId = _siteMapper.SiteId;

            expirationTime = DateTime.MaxValue;

            if (siteId.HasValue)
            {
                LoadDatabaseResources(siteId.Value);
            }
        }

        public bool Expires(EntityType entityType, long entityId, EntityChangeType changeType)
        {
            return entityType == EntityType.SiteCustomization;
        }

        private readonly EntityType[] _handledTypes = { EntityType.SiteCustomization };

        public EntityType[] HandledTypes
        {
            get
            {
                return _handledTypes;
            }
        }
    }
}
