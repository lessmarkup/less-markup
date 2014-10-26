/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Configuration;
using System.Web.Razor;
using System.Web.Razor.Parser.SyntaxTree;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Exceptions;
using LessMarkup.Interfaces.Module;
using LessMarkup.Interfaces.System;
using Microsoft.CSharp;

namespace LessMarkup.Engine.Build.View
{
    class ViewBuilder
    {
        #region Private Fields

        private readonly CSharpCodeProvider _codeProvider = new CSharpCodeProvider();
        private readonly List<Assembly> _referencedAssemblies = new List<Assembly>(); 
        private readonly List<RazorError> _compileErrors = new List<RazorError>();
        private readonly List<string> _defaultNamespaces = new List<string>(); 

        private readonly List<string> _compiledCode = new List<string>();
        private readonly ISpecialFolder _specialFolder;
        private readonly IModuleProvider _moduleProvider;
        private readonly string _compileDirectory;

        private readonly object _buildLock = new object();

        #endregion

        public ViewBuilder(ISpecialFolder specialFolder, IModuleProvider moduleProvider)
        {
            _compileDirectory = Path.Combine(specialFolder.ApplicationData, "Compiled");

            _referencedAssemblies.Add(typeof(DotNetOpenAuth.AspNet.IAuthenticationClient).Assembly);
            _referencedAssemblies.Add(typeof(Microsoft.CSharp.RuntimeBinder.Binder).Assembly);

            _specialFolder = specialFolder;
            _moduleProvider = moduleProvider;
        }

        public static void ExtractPageClassName(ViewTemplate template, out string pageNamespace, out string pageClassName)
        {
            pageNamespace = "";
            pageClassName = template.Name;

            var pos = template.Name.LastIndexOf('.');

            if (pos > 0)
            {
                pageNamespace = template.Name.Substring(0, pos);
                pageClassName = template.Name.Substring(pos + 1);
            }

            if (!string.IsNullOrEmpty(pageNamespace))
            {
                pageNamespace = "." + pageNamespace;
            }

            if (!string.IsNullOrWhiteSpace(template.Namespace))
            {
                pageNamespace = template.Namespace + ".Views" + pageNamespace;
            }
            else
            {
                pageNamespace = "LessMarkup" + pageNamespace;
            }
        }

        void CompileViewTemplate(ViewTemplate template)
        {
            var templatePath = "~/Views/" + template.Name.Replace('.', '/') + ".cshtml";

            var razorHost = new PageHost(templatePath, null)
            {
                DefaultNamespace = "LessMarkup",
                DefaultPageBaseClass = typeof(ViewPage).FullName
            };

#if DEBUG
            razorHost.DefaultDebugCompilation = true;
#endif

            foreach (var ns in _defaultNamespaces)
            {
                razorHost.NamespaceImports.Add(ns);
            }

            if (!string.IsNullOrWhiteSpace(template.Namespace))
            {
                razorHost.NamespaceImports.Add(template.Namespace);
            }

            var templateEngine = new RazorTemplateEngine(razorHost);
            GeneratorResults results;

            string pageNamespace, pageClassName;

            ExtractPageClassName(template, out pageNamespace, out pageClassName);

            using (var reader = new StringReader(template.Body))
            {
                results = templateEngine.GenerateCode(reader, pageClassName, pageNamespace, templatePath);
            }

            if (!results.Success)
            {
                _compileErrors.AddRange(results.ParserErrors);
                return;
            }

            var builder = new StringBuilder();
            using (var writer = new StringWriter(builder))
            {
                _codeProvider.GenerateCodeFromCompileUnit(results.GeneratedCode, writer, new CodeGeneratorOptions());
            }

            var filePath = Path.Combine(_compileDirectory, template.Name + ".cshtml");
            File.WriteAllText(filePath, builder.ToString());
            _compiledCode.Add(filePath);
        }

        private void BuildViews(ViewImport viewImport, string outputPath)
        {
            this.LogDebug("Building views to " + outputPath);
            this.LogDebug("Using compile directory " + _compileDirectory);
            if (Directory.Exists(_compileDirectory))
            {
                Directory.Delete(_compileDirectory, true);
            }
            Directory.CreateDirectory(_compileDirectory);

            var pagesSection = (PagesSection)WebConfigurationManager.GetSection("system.web/pages");

            foreach (NamespaceInfo ns in pagesSection.Namespaces)
            {
                _defaultNamespaces.Add(ns.Namespace);
            }

            this.LogDebug("Using default namespaces " + string.Join(",", _defaultNamespaces));

            foreach (var template in viewImport.ViewTemplates)
            {
                CompileViewTemplate(template.Value);
            }

            if (_compileErrors.Any())
            {
                throw new CompileException(_compileErrors.Select(e => e.Message).ToList());
            }

            var compilerParameters = new CompilerParameters();

            var locations = new Dictionary<string, string>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic))
            {
                locations[Path.GetFileName(assembly.Location)] = assembly.Location;
            }

            foreach (var assembly in _referencedAssemblies)
            {
                locations[Path.GetFileName(assembly.Location)] = assembly.Location;
            }

            foreach (var module in _moduleProvider.Modules)
            {
                var path = new Uri(module.Assembly.CodeBase).LocalPath;

                var pos = path.LastIndexOf('\\');

                if (pos > 0)
                {
                    path = path.Substring(0, pos);
                }

                foreach (var assemblyName in module.Assembly.GetReferencedAssemblies())
                {
                    var assemblyPath = Path.Combine(path, assemblyName.Name + ".dll");

                    var assembly = File.Exists(assemblyPath) ? Assembly.LoadFile(assemblyPath) : Assembly.Load(assemblyName);

                    locations[Path.GetFileName(assembly.Location)] = assembly.Location;
                }
            }

            foreach (var location in locations)
            {
                compilerParameters.ReferencedAssemblies.Add(location.Value);
            }

            compilerParameters.GenerateInMemory = false;
#if DEBUG
            compilerParameters.IncludeDebugInformation = true;
#endif
            compilerParameters.GenerateExecutable = false;
            compilerParameters.CompilerOptions = "/target:library";
#if !DEBUG
            compilerParameters.CompilerOptions += " /optimize";
#endif
            compilerParameters.OutputAssembly = outputPath;

            var outputDirectory = Path.GetDirectoryName(outputPath);

            if (!string.IsNullOrEmpty(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var results = _codeProvider.CompileAssemblyFromFile(compilerParameters, _compiledCode.ToArray());

            if (results.Errors != null && results.Errors.HasErrors)
            {
                var errors = new List<string>();

                foreach (var error in results.Errors.Cast<CompilerError>().Where(e => !e.IsWarning))
                {
                    var errorText = string.Format("Error {0}: {1} in line {2}, file {3}", error.ErrorNumber, error.ErrorText, error.Line, Path.GetFileName(error.FileName));
                    if (File.Exists(error.FileName))
                    {
                        var lines = File.ReadAllLines(error.FileName);
                        if (lines.Length >= error.Line)
                        {
                            errorText += "\r\n> " + lines[error.Line - 1].Trim();
                        }
                    }
                    errors.Add(errorText);
                }

                var errorsPath = Path.Combine(_compileDirectory, "errors.txt");
                File.Delete(errorsPath);
                File.WriteAllLines(errorsPath, errors);
                throw new CompileException(errors);
            }
        }

        public bool IsActive
        {
            get
            {
                return File.Exists(_specialFolder.GeneratedViewAssembly);
            }
        }

        public bool IsRecent
        {
            get
            {
                if (!IsActive || !File.Exists(_specialFolder.GeneratedViewAssembly))
                {
                    return false;
                }

                var fileDate = File.GetLastWriteTimeUtc(_specialFolder.GeneratedViewAssembly);

                return _moduleProvider.Modules
                    .Select(module => File.GetLastWriteTimeUtc(new Uri(module.Assembly.CodeBase).LocalPath))
                    .All(moduleDate => moduleDate <= fileDate);
            }
        }

        public DateTime LastBuildTime
        {
            get
            {
                if (!File.Exists(_specialFolder.GeneratedViewAssembly))
                {
                    return DateTime.MinValue;
                }
                return File.GetLastWriteTimeUtc(_specialFolder.GeneratedViewAssembly);
            }
        }

        public void Activate()
        {
            Directory.CreateDirectory(_specialFolder.GeneratedAssemblies);

            foreach (var directory in new DirectoryInfo(_specialFolder.GeneratedAssemblies).GetDirectories("Site*"))
            {
                directory.Delete(true);
            }

            if (!File.Exists(_specialFolder.GeneratedViewAssemblyNew))
            {
                throw new FileNotFoundException();
            }
            if (File.Exists(_specialFolder.GeneratedViewAssembly))
            {
                File.Delete(_specialFolder.GeneratedViewAssembly);
            }
            File.Move(_specialFolder.GeneratedViewAssemblyNew, _specialFolder.GeneratedViewAssembly);
        }

        public void Build()
        {
            lock (_buildLock)
            {
                File.Delete(_specialFolder.GeneratedViewAssemblyNew);

                var viewImport = new ViewImport();

                foreach (var module in _moduleProvider.Modules)
                {
                    viewImport.ImportModule(module, false);
                }

                BuildViews(viewImport, _specialFolder.GeneratedViewAssemblyNew);
            }
        }

        public void ImportTemplates(IDomainModelProvider domainModelProvider)
        {
            var viewImport = new ViewImport();

            foreach (var module in _moduleProvider.Modules)
            {
                viewImport.ImportModule(module, true);
            }
        }
    }
}
