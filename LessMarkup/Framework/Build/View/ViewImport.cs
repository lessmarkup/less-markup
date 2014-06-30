/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LessMarkup.Interfaces.Module;

namespace LessMarkup.Framework.Build.View
{
    public class ViewImport
    {
        private readonly Dictionary<string, ViewTemplate> _viewTemplates = new Dictionary<string, ViewTemplate>();
        private readonly Dictionary<string, ContentTemplate> _contentTemplates = new Dictionary<string, ContentTemplate>();

        public Dictionary<string, ViewTemplate> ViewTemplates { get { return _viewTemplates; } }
        public Dictionary<string, ContentTemplate> ContentTemplates { get { return _contentTemplates; } }

        private void ImportPage(Stream stream, string name, ModuleConfiguration module)
        {
            string body;

            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                body = reader.ReadToEnd();
            }

            var viewTemplate = new ViewTemplate
            {
                Body = body, 
                Namespace = module != null ? module.Namespace : null, 
                Name = name,
                Path = "/Views/" + name.Replace('.', '/') + ".cshtml"
            };

            _viewTemplates[name] = viewTemplate;
        }

        private void ImportContent(string resourceName, string name, ModuleConfiguration module)
        {
            var parts = name.Split(new[] { '.' });

            if (parts.Length < 2)
            {
                return;
            }

            /*var extension = parts[parts.Length - 1];

            bool isBinaryFile = false;

            switch (extension)
            {
                case "png":
                case "gif":
                case "ico":
                case "otf":
                case "eot":
                case "swg":
                case "ttf":
                case "woff":
                    isBinaryFile = true;
                    break;
                case "js":
                case "css":
                    break;
                default:
                    return;
            }*/

            var template = new ContentTemplate();

            if (name.EndsWith("favicon.ico"))
            {
                template.Name = "favicon.ico";
            }
            else
            {
                var folder = parts[0];

                if (!string.IsNullOrWhiteSpace(module.Name))
                {
                    template.Name = folder + "/" + module.Name;
                }
                else
                {
                    template.Name = folder;
                }

                for (int i = 1; i < parts.Length; i++)
                {
                    if (i < parts.Length - 1 && char.IsUpper(parts[i - 1][0]))
                    {
                        template.Name += "/";
                    }
                    else
                    {
                        template.Name += ".";
                    }

                    template.Name += parts[i];
                }
            }

            using (var stream = module.Assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    return;
                }

                using (var reader = new BinaryReader(stream))
                {
                    template.Binary = reader.ReadBytes((int)stream.Length);
                }
            }

            _contentTemplates[template.Name] = template;
        }

        public void ImportModule(ModuleConfiguration module, bool importContent)
        {
            var moduleNamespace = module.Namespace + ".";

            foreach (var resourceName in module.Assembly.GetManifestResourceNames())
            {
                var pos = resourceName.IndexOf(".Views.", StringComparison.Ordinal);
                
                if (pos <= 0)
                {
                    if (importContent && resourceName.StartsWith(moduleNamespace))
                    {
                        var contentName = resourceName.Substring(moduleNamespace.Length);
                        ImportContent(resourceName, contentName, module);
                    }
                    continue;
                }

                var name = resourceName.Substring(pos + ".Views.".Length);

                pos = name.LastIndexOf('.');

                if (pos < 0)
                {
                    continue;
                }

                var extension = name.Substring(pos);
                name = name.Remove(pos);

                if (extension != ".cshtml" || string.IsNullOrEmpty(name))
                {
                    continue;
                }

                using (var inputStream = module.Assembly.GetManifestResourceStream(resourceName))
                {
                    if (inputStream != null)
                    {
                        ImportPage(inputStream, name, module);
                    }
                }
            }
        }
    }
}
