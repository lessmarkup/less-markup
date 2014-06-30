/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Configuration;
using LessMarkup.DataFramework;
using LessMarkup.DataFramework.DataAccess;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Exceptions;
using LessMarkup.Interfaces.System;
using Microsoft.CSharp;

namespace LessMarkup.Framework.Build.Data
{
    class DataBuilder
    {
        #region Private Fields

        private readonly List<Assembly> _linkedAssemblies = new List<Assembly>();
        private readonly Dictionary<string, PropertyInfo> _domainModelProperties = new Dictionary<string, PropertyInfo>();
        private readonly List<Type> _modelCreateTypes = new List<Type>(); 
        private readonly ISpecialFolder _specialFolder;

        private readonly Func<IEnumerable<FileInfo>, IEnumerable<FileInfo>>  _filesFilter =
            fl => fl.Where(f => f.Name.Contains(".DataAccess.") && f.Name.EndsWith(".dll"));

        #endregion

        #region Initialization

        public DataBuilder(ISpecialFolder specialFolder)
        {
            _specialFolder = specialFolder;
        }

        #endregion

        public bool IsActive
        {
            get
            {
                return File.Exists(_specialFolder.GeneratedDataAssembly);
            }
        }

        public bool IsRecent
        {
            get
            {
                if (!IsActive || !File.Exists(_specialFolder.GeneratedDataAssembly))
                {
                    return false;
                }

                var compiledTime = File.GetLastWriteTime(_specialFolder.GeneratedDataAssembly);

                foreach (var sourceFile in DataAccessFiles)
                {
                    var sourceTime = File.GetLastWriteTime(sourceFile.FullName);
                    if (sourceTime > compiledTime)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public DateTime LastBuildTime
        {
            get
            {
                if (!File.Exists(_specialFolder.GeneratedDataAssembly))
                {
                    return DateTime.MinValue;
                }
                return File.GetLastWriteTimeUtc(_specialFolder.GeneratedDataAssembly);
            }
        }

        public void Build()
        {
            DiscoverTypes();
            Generate();
        }

        public void Activate()
        {
            if (!File.Exists(_specialFolder.GeneratedDataAssemblyNew))
            {
                throw new FileNotFoundException();
            }
            var outputPath = Path.GetDirectoryName(_specialFolder.GeneratedDataAssembly);
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new NoNullAllowedException();
            }
            Directory.CreateDirectory(outputPath);
            File.Delete(_specialFolder.GeneratedDataAssembly);
            File.Move(_specialFolder.GeneratedDataAssemblyNew, _specialFolder.GeneratedDataAssembly);
        }

        private IEnumerable<FileInfo> DataAccessFiles
        {
            get
            {
                return _filesFilter(new DirectoryInfo(_specialFolder.BinaryFiles).GetFiles("*.dll"));
            }
        }

        void Generate()
        {
            Directory.CreateDirectory(_specialFolder.GeneratedAssemblies);
            File.Delete(_specialFolder.GeneratedDataAssemblyNew);

            var compileUnit = new CodeCompileUnit();
            const string targetNamespace = Constants.DataAccessGenerator.DefaultNamespace;
            var codeNamespace = new CodeNamespace(targetNamespace);

            foreach (var ns in Constants.DataAccessGenerator.UsingNamespaces)
            {
                codeNamespace.Imports.Add(new CodeNamespaceImport(ns));
            }

            var domainModelType = new CodeTypeDeclaration(Constants.DataAccessGenerator.DomainModelClassName)
                {
                    Attributes = MemberAttributes.Public
                };
            domainModelType.BaseTypes.Add(typeof (AbstractDomainModel));
            var domainModelConstructor = new CodeConstructor {Attributes = MemberAttributes.Public};
            var codeMethodReference = new CodeMethodReferenceExpression(new CodeBaseReferenceExpression(), "AddModelCreate");

            foreach (var type in _modelCreateTypes)
            {
                var expression = new CodeMethodInvokeExpression(codeMethodReference, new CodeExpression[] { new CodeTypeOfExpression(type) });
                domainModelConstructor.Statements.Add(expression);
            }

            domainModelType.Members.Add(domainModelConstructor);

            var staticDomainModelConstructor = new CodeTypeConstructor {Attributes = MemberAttributes.Static};

            var setExpression = new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("Database"),
                "SetInitializer",
                new CodeExpression[]
                {
                    new CodeObjectCreateExpression(
                        new CodeTypeReference("MigrateDatabaseToLatestVersion", new CodeTypeReference("DomainModel"),
                            new CodeTypeReference("Configuration")))
                });
            staticDomainModelConstructor.Statements.Add(setExpression);

            domainModelType.Members.Add(staticDomainModelConstructor);

            foreach (var source in _domainModelProperties.Values)
            {
                var fieldName = "_field" + source.Name;

                var field = new CodeMemberField
                    {
                        Attributes = MemberAttributes.Private,
                        Name = fieldName,
                        Type = new CodeTypeReference(source.PropertyType)
                    };
                domainModelType.Members.Add(field);

                var property = new CodeMemberProperty
                {
                    Name = source.Name, 
                    Attributes = MemberAttributes.Public, 
                    HasGet = true, 
                    HasSet = true, 
                    Type = new CodeTypeReference(source.PropertyType)
                };
                property.GetStatements.Add(new CodeMethodReturnStatement(new CodeSnippetExpression(fieldName)));
                property.SetStatements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName), new CodePropertySetValueReferenceExpression()));
                //property.Name += " {get;set;}//";
                domainModelType.Members.Add(property);

                setExpression = new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("AbstractDomainModel"), "AddProperty", new CodeTypeOfExpression("DomainModel"), new CodeSnippetExpression("\"" + source.Name + "\""));
                staticDomainModelConstructor.Statements.Add(setExpression);
            }

            codeNamespace.Types.Add(domainModelType);

            var configurationType = new CodeTypeDeclaration("Configuration") { Attributes = MemberAttributes.Public };
            configurationType.BaseTypes.Add(new CodeTypeReference("DbMigrationsConfiguration", new CodeTypeReference("DomainModel")));

            var configurationConstructor = new CodeConstructor { Attributes = MemberAttributes.Public };
            configurationType.Members.Add(configurationConstructor);

            codeNamespace.Types.Add(configurationType);

            compileUnit.Namespaces.Add(codeNamespace);

            var codeProvider = new CSharpCodeProvider();

            var builder = new StringBuilder();
            using (var writer = new StringWriter(builder))
            {
                codeProvider.GenerateCodeFromCompileUnit(compileUnit, writer, new CodeGeneratorOptions());
            }

            var compilerParameters = new CompilerParameters();

            var assemblyMap = new Dictionary<string, string>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic))
            {
                assemblyMap[Path.GetFileName(assembly.Location)] = assembly.Location;
            }

            foreach (var item in assemblyMap)
            {
                compilerParameters.ReferencedAssemblies.Add(item.Value);
            }

            compilerParameters.GenerateInMemory = false;
            compilerParameters.IncludeDebugInformation = false;
            compilerParameters.GenerateExecutable = false;
            compilerParameters.CompilerOptions = "/target:library /optimize";
            compilerParameters.OutputAssembly = _specialFolder.GeneratedDataAssemblyNew;
            var outputPath = Path.GetDirectoryName(compilerParameters.OutputAssembly);
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new NoNullAllowedException();
            }
            Directory.CreateDirectory(outputPath);

            var viewBuilderLogDirectory = WebConfigurationManager.AppSettings["ViewBuilderLogDirectory"];

            if (!string.IsNullOrWhiteSpace(viewBuilderLogDirectory) && Directory.Exists(viewBuilderLogDirectory))
            {
                var path = Path.Combine(viewBuilderLogDirectory, "DataAccess.cs");
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                File.WriteAllText(path, builder.ToString());
            }

            var compileResults = codeProvider.CompileAssemblyFromSource(compilerParameters, new[] { builder.ToString() });

            if (compileResults.Errors != null && compileResults.Errors.Count > 0)
            {
                var errors = new List<string>();
                var lines = new List<string>();

                using (var reader = new StringReader(builder.ToString()))
                {
                    for (;;)
                    {
                        var line = reader.ReadLine();
                        if (line == null)
                        {
                            break;
                        }
                        
                        lines.Add(line);
                    }
                }

                foreach (CompilerError error in compileResults.Errors)
                {
                    var errorText = string.Format("Error {0}: {1} in line {2}", error.ErrorNumber, error.ErrorText, error.Line);
                    if (lines.Count >= error.Line)
                    {
                        errorText += "\r\n> " + lines[error.Line - 1].Trim();
                    }
                    errors.Add(errorText);
                }

                throw new CompileException(errors);
            }
        }

        void DiscoverTypes()
        {
            _linkedAssemblies.Add(Assembly.GetExecutingAssembly());

            foreach (var sourceFile in DataAccessFiles)
            {
                var assembly = Assembly.LoadFrom(sourceFile.FullName);
                _linkedAssemblies.Add(assembly);

                foreach (var sourceType in assembly.GetTypes())
                {
                    if (typeof (DbContext).IsAssignableFrom(sourceType))
                    {
                        foreach (var property in sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                        {
                            if (!property.PropertyType.IsGenericType || property.PropertyType.Name != typeof(DbSet<>).Name)
                            {
                                continue;
                            }

                            _domainModelProperties[property.PropertyType.FullName] = property;
                        }
                    }

                    if (typeof (IModelCreate).IsAssignableFrom(sourceType) && !sourceType.IsInterface)
                    {
                        _modelCreateTypes.Add(sourceType);
                    }
                }
            }
        }
    }
}
