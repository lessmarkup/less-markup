/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using LessMarkup.DataFramework;
using LessMarkup.Engine.FileSystem;
using LessMarkup.Interfaces.Module;

namespace LessMarkup.Engine.Minify
{
    internal class ResourceMinifer
    {
        private readonly IModuleProvider _moduleProvider;
        private readonly List<ResourceReference> _jsToMinify = new List<ResourceReference>();
        private readonly List<ResourceReference> _cssToMinify = new List<ResourceReference>();

        public ResourceMinifer(IModuleProvider moduleProvider)
        {
            _moduleProvider = moduleProvider;
        }

        public void Minify(Dictionary<string, ResourceReference> references)
        {
            var assemblies = new List<Assembly>
            {
                _moduleProvider.Modules.Single(m => m.ModuleType == Constants.ModuleType.UserInterface).Assembly,
                _moduleProvider.Modules.Single(m => m.ModuleType == Constants.ModuleType.MainModule).Assembly
            };

            assemblies.AddRange(_moduleProvider.Modules.Where(m => !m.System).Select(m => m.Assembly));

            var serializer = new XmlSerializer(typeof (XmlMinifyFile));

            foreach (var assembly in assemblies)
            {
                LoadAssemblyConfiguration(references, assembly, serializer);
            }

            var jsContent = "";

            if (_jsToMinify.Count > 0)
            {
                jsContent = MinifyContent(_jsToMinify, jsContent, true);
            }

            references.Add(Constants.Minify.JsMinify, new ResourceReference
            {
                Binary = Encoding.UTF8.GetBytes(jsContent)
            });

            var cssContext = "";

            if (_cssToMinify.Count > 0)
            {
                cssContext = MinifyContent(_cssToMinify, cssContext, false);
            }

            references.Add(Constants.Minify.CssMinify, new ResourceReference
            {
                Binary = Encoding.UTF8.GetBytes(cssContext)
            });
        }

        private string MinifyContent(IEnumerable<ResourceReference> toMinify, string initialContent, bool minifyJs)
        {
            var content = new StringBuilder();
            content.AppendLine(initialContent);

#if !DEBUG
            var minify = minifyJs ? (IMinify) new JsMinify() : new CssMinify();
#endif

            foreach (var resource in toMinify)
            {
                var source = Encoding.UTF8.GetString(resource.Binary);
#if DEBUG
                content.AppendLine("/************************");
                content.AppendLine("/");
                content.AppendLine("/ " + resource.Path);
                content.AppendLine("/");
                content.AppendLine("/*************************/");
                var target = source;
#else
                var target = resource.Minified ? source : minify.Process(source);
#endif
                content.AppendLine(target);
            }

            foreach (var resource in _jsToMinify)
            {
                resource.Binary = new byte[0];
            }

            return content.ToString();
        }

        private void LoadAssemblyConfiguration(Dictionary<string, ResourceReference> references, Assembly assembly, XmlSerializer serializer)
        {
            var minifyConfiguration = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("Minify.xml"));
            if (minifyConfiguration == null)
            {
                return;
            }

            using (var stream = assembly.GetManifestResourceStream(minifyConfiguration))
            {
                if (stream == null)
                {
                    return;
                }

                using (var reader = new XmlTextReader(stream))
                {
                    var resourceFile = (XmlMinifyFile) serializer.Deserialize(reader);
                    if (resourceFile.Resources == null)
                    {
                        return;
                    }

                    foreach (var resource in resourceFile.Resources)
                    {
#if DEBUG
                        var resource1 = resource.Plain;
                        var minified = false;
                        if (string.IsNullOrWhiteSpace(resource1))
                        {
                            resource1 = resource.Minified;
                            minified = true;
                        }
#else
                        var resource1 = resource.Minified;
                        var minified = true;
                        if (string.IsNullOrWhiteSpace(resource1))
                        {
                            resource1 = resource.Plain;
                            minified = false;
                        }
#endif

                        if (string.IsNullOrWhiteSpace(resource1))
                        {
                            continue;
                        }

                        foreach (var reference in references.Where(r => r.Key.StartsWith(resource1)))
                        {
                            var extension = Path.GetExtension(reference.Key);
                            if (extension == null)
                            {
                                continue;
                            }

                            switch (extension.ToLower())
                            {
                                case ".js":
                                    reference.Value.Minified = minified;
                                    reference.Value.Path = resource1;
                                    _jsToMinify.Add(reference.Value);
                                    break;
                                case ".css":
                                    reference.Value.Minified = minified;
                                    reference.Value.Path = resource1;
                                    _cssToMinify.Add(reference.Value);
                                    break;
                            }
                        }
                    }
                }
            }
        }
    }
}
