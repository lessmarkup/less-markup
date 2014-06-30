/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Data;
using System.Web;
using LessMarkup.Interfaces.System;

namespace LessMarkup.Framework.FileSystem
{
    class SpecialFolder : ISpecialFolder
    {
        public static string ApplicationDataFolder
        {
            get
            {
                var folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                if (string.IsNullOrWhiteSpace(folderPath))
                {
                    throw new NoNullAllowedException("ApplicationDataFolder");
                }
                return System.IO.Path.Combine(folderPath, "LessMarkup");
            }
        }

        public string ApplicationData
        {
            get
            {
                return ApplicationDataFolder;
            }
        }

        public string GeneratedAssemblies
        {
            get { return System.IO.Path.Combine(ApplicationData, "GeneratedAssemblies"); }
        }

        public string BinaryFiles
        {
            get { return HttpRuntime.BinDirectory; }
        }

        public string RootFolder
        {
            get { return HttpRuntime.AppDomainAppPath; }            
        }

        public string GeneratedDataAssembly
        {
            get { return System.IO.Path.Combine(GeneratedAssemblies, "DataAccessGen.dll"); }
        }

        public string GeneratedDataAssemblyNew
        {
            get { return System.IO.Path.Combine(GeneratedAssemblies, "DataAccessGenNew.dll"); }
        }

        public string GeneratedViewAssembly
        {
            get { return System.IO.Path.Combine(GeneratedAssemblies, "ViewGen.dll"); }
        }

        public string GeneratedViewAssemblyNew
        {
            get { return System.IO.Path.Combine(GeneratedAssemblies, "ViewGenNew.dll"); }
        }
    }
}
