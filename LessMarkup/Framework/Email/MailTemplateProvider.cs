/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Reflection;
using LessMarkup.DataFramework;
using LessMarkup.Interfaces.System;

namespace LessMarkup.Framework.Email
{
    class MailTemplateProvider : IMailTemplateProvider
    {
        private readonly ISpecialFolder _specialFolder;
        private Assembly _assembly;

        public MailTemplateProvider(ISpecialFolder specialFolder)
        {
            _specialFolder = specialFolder;
        }

        public string ExecuteTemplate<T>(string templateId, T model)
        {
            if (_assembly == null)
            {
                _assembly = Assembly.LoadFile(_specialFolder.GeneratedViewAssembly);
            }

            using (var compiledInstance = (IMailTemplate<T>) _assembly.CreateInstance(Constants.MailTemplates.Namespace + "." + templateId))
            {
                if (compiledInstance == null)
                {
                    throw new TypeLoadException();
                }

                return compiledInstance.Execute(model);
            }
        }
    }
}
