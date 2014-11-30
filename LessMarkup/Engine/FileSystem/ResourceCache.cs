/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using LessMarkup.Engine.ResourceTemplate;
using LessMarkup.Interfaces;
#if DEBUG
using System.Diagnostics;
#endif
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using LessMarkup.DataObjects.Common;
using LessMarkup.Engine.Build.View;
using LessMarkup.Engine.Minify;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Module;
using LessMarkup.Interfaces.System;

namespace LessMarkup.Engine.FileSystem
{
    class ResourceCache : AbstractCacheHandler, IResourceCache
    {
        private readonly IDomainModelProvider _domainModelProvider;
        private readonly ISpecialFolder _specialFolder;
        private readonly IModuleProvider _moduleProvider;
        private readonly IEngineConfiguration _engineConfiguration;
        private readonly object _loadLock = new object();

        private Assembly _globalAssembly;

        private readonly Dictionary<string, ResourceReference> _resourceReferences = new Dictionary<string, ResourceReference>();
        private readonly Dictionary<string, ViewReference> _viewReferences = new Dictionary<string, ViewReference>();

        public ResourceCache(IDomainModelProvider domainModelProvider, ISpecialFolder specialFolder, IModuleProvider moduleProvider, IEngineConfiguration engineConfiguration)
            : base(new[] { typeof(SiteCustomization), typeof(DataObjects.Common.Language) })
        {
            _domainModelProvider = domainModelProvider;
            _specialFolder = specialFolder;
            _moduleProvider = moduleProvider;
            _engineConfiguration = engineConfiguration;
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
#if DEBUG
            Trace.WriteLine("CheckExists: " + path);
#endif
            return _resourceReferences.ContainsKey(ExtractPath(path).ToLower());
        }

        private ResourceReference LoadResource(string path)
        {
#if DEBUG
            Trace.WriteLine("LoadResource: " + path);
#endif
            ResourceReference reference;
            if (!_resourceReferences.TryGetValue(path.ToLower(), out reference))
            {
                return null;
            }

            return reference;
        }

        internal ResourceReference GetResourceReference(string path)
        {
            return LoadResource(ExtractPath(path).ToLower());
        }

        internal void AddResourceReference(string path, ResourceReference reference)
        {
            _resourceReferences.Add(path, reference);
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
#if DEBUG
            Trace.WriteLine("DirectoryExists: " + path);
#endif
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
#if DEBUG
            Trace.WriteLine("GetFiles: " + path);
#endif
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

        public Type LoadType(string path)
        {
#if DEBUG
            Trace.WriteLine("LoadType: " + path);
#endif

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

        private void ImportTemplateRecord(string recordId, string path, byte[] resource, bool isView, string moduleType)
        {
            if (resource == null)
            {
                throw new ArgumentOutOfRangeException("resource");
            }

            if (isView)
            {
                _viewReferences[path] = new ViewReference
                {
                    ClassName = recordId,
                    IsSiteAssembly = false,
                    Path = path,
                    ModuleType = moduleType
                };
            }
            else
            {
                _resourceReferences[path.ToLower()] = new ResourceReference
                {
                    Binary = resource,
                    ModuleType = moduleType
                };
            }
        }

        private void ImportViewTemplate(ViewTemplate viewTemplate)
        {
            string pageNamespace, pageClassName;
            ViewBuilder.ExtractPageClassName(viewTemplate, out pageNamespace, out pageClassName);
            var className = pageNamespace + "." + pageClassName;
            ImportTemplateRecord(className, viewTemplate.Path, Encoding.UTF8.GetBytes(viewTemplate.Body), true, viewTemplate.ModuleType);
        }

        private void ImportContentTemplate(ContentTemplate contentTemplate)
        {
            ImportTemplateRecord(contentTemplate.Name, contentTemplate.Name, contentTemplate.Binary, false, contentTemplate.ModuleType);
        }

        private void LoadDatabaseResources()
        {
            if (_engineConfiguration.DisableCustomizations)
            {
                return;
            }

            using (var domainModel = _domainModelProvider.Create())
            {
                foreach (var record in domainModel.Query().From<SiteCustomization>().ToList<SiteCustomization>())
                {
                    var recordPath = record.Path.ToLower();

                    ResourceReference resourceReference;

                    if (record.Append && _resourceReferences.TryGetValue(recordPath, out resourceReference))
                    {
                        var binary = new byte[resourceReference.Binary.Length + record.Body.Length];
                        Buffer.BlockCopy(resourceReference.Binary, 0, binary, 0, resourceReference.Binary.Length);
                        Buffer.BlockCopy(record.Body, 0, binary, resourceReference.Binary.Length, record.Body.Length);
                        resourceReference.Binary = binary;
                        continue;
                    }

                    resourceReference = new ResourceReference
                    {
                        RecordId = record.Id,
                        Binary = record.Body,
                    };

                    _resourceReferences[record.Path.ToLower()] = resourceReference;
                }
            }
        }

        protected override void Initialize(long? objectId)
        {
            _globalAssembly = Assembly.LoadFile(_specialFolder.GeneratedViewAssembly);

            var viewImport = new ViewImport();
            foreach (var module in _moduleProvider.Modules)
            {
                viewImport.ImportModule(module, true);
            }

            foreach (var template in viewImport.ViewTemplates)
            {
                ImportViewTemplate(template.Value);
            }

            foreach (var resource in viewImport.ContentTemplates)
            {
                ImportContentTemplate(resource.Value);
            }

            LoadDatabaseResources();

            var resourceTemplateParser = DependencyResolver.Resolve<ResourceTemplateParser>();
            var results = new Dictionary<ResourceReference, string>();

            foreach (var reference in _resourceReferences)
            {
                var ext = (Path.GetExtension(reference.Key) ?? "").ToLower();

                if (ext == ".js" || ext == ".html" || ext == ".cshtml")
                {
                    var result = resourceTemplateParser.GetTemplate(objectId, reference.Key, reference.Value, this);
                    if (!string.IsNullOrWhiteSpace(result))
                    {
                        results[reference.Value] = result;
                    }
                }
            }

            foreach (var result in results)
            {
                result.Key.Binary = Encoding.UTF8.GetBytes(result.Value);
            }

            var minifier = DependencyResolver.Resolve<ResourceMinifer>();
            minifier.Minify(_resourceReferences, this);
        }
    }
}
