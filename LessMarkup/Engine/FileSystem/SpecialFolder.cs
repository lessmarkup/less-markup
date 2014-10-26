/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.IO;
using System.Reflection;
using System.Security;
using System.Web;
using LessMarkup.Interfaces.System;

namespace LessMarkup.Engine.FileSystem
{
    class SpecialFolder : ISpecialFolder
    {
        private static readonly string _applicationDataFolder;

        static SpecialFolder()
        {
            try
            {
                var folderPath = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;

                var pos = folderPath.LastIndexOf('\\');

                if (pos <= 0)
                {
                    throw new Exception("Cannot parse folder name '" + folderPath + "'");
                }

                folderPath = folderPath.Substring(0, pos);
                folderPath = Path.GetDirectoryName(folderPath);

                if (folderPath == null)
                {
                    throw new NullReferenceException("folderPath");
                }

                if (folderPath.EndsWith("\bin") || folderPath.EndsWith("/bin"))
                {
                    folderPath = folderPath.Substring(0, folderPath.Length - "\bin".Length);
                }

                if (!Directory.Exists(Path.Combine(folderPath, "ApplicationData")))
                {
                    var parentFolderPath = Path.GetDirectoryName(folderPath);

                    if (parentFolderPath != null && Directory.Exists(Path.Combine(parentFolderPath, "ApplicationData")))
                    {
                        folderPath = parentFolderPath;
                    }
                }

                _applicationDataFolder = Path.Combine(folderPath, "ApplicationData");

                Directory.CreateDirectory(_applicationDataFolder);
            }
            catch (Exception e)
            {
                throw new SecurityException("Failed to access or create ApplicationData folder", e);
            }
        }

        public static string ApplicationDataFolder
        {
            get { return _applicationDataFolder; }
        }

        public string LogFolder
        {
            get { return Path.Combine(ApplicationDataFolder, "Log"); }
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
            get { return Path.Combine(ApplicationData, "GeneratedAssemblies"); }
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
            get { return Path.Combine(GeneratedAssemblies, "DataAccessGen.dll"); }
        }

        public string GeneratedDataAssemblyNew
        {
            get { return Path.Combine(GeneratedAssemblies, "DataAccessGenNew.dll"); }
        }

        public string GeneratedViewAssembly
        {
            get { return Path.Combine(GeneratedAssemblies, "ViewGen.dll"); }
        }

        public string GeneratedViewAssemblyNew
        {
            get { return Path.Combine(GeneratedAssemblies, "ViewGenNew.dll"); }
        }
    }
}
