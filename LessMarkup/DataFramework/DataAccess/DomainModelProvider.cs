/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Reflection;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.System;

namespace LessMarkup.DataFramework.DataAccess
{
    class DomainModelProvider : IDomainModelProvider, IInitialize
    {
        private ConstructorInfo _constructor;
        private readonly ISpecialFolder _specialFolder;
        private readonly ISiteMapper _siteMapper;
        private readonly IEngineConfiguration _engineConfiguration;
        private bool _initialized;

        public DomainModelProvider(ISpecialFolder specialFolder, ISiteMapper siteMapper, IEngineConfiguration engineConfiguration)
        {
            _specialFolder = specialFolder;
            _siteMapper = siteMapper;
            _engineConfiguration = engineConfiguration;
        }

        public void Initialize(params object[] args)
        {
            var dataAccessAssembly = Assembly.LoadFile(_specialFolder.GeneratedDataAssembly);
            var domainModelType = dataAccessAssembly.GetType(Constants.DataAccessGenerator.DefaultNamespace + "." + Constants.DataAccessGenerator.DomainModelClassName, true);
            _constructor = domainModelType.GetConstructor(Type.EmptyTypes);

            if (args.Length > 0 && (bool)args[0])
            {
                _initialized = true;
                return;
            }

            var configurationType = dataAccessAssembly.GetType(Constants.DataAccessGenerator.DefaultNamespace + ".Configuration", true);
            var migrationType = typeof (MigrateDatabaseToLatestVersion<,>).MakeGenericType(domainModelType, configurationType);

            var constructorInfo = migrationType.GetConstructor(Type.EmptyTypes);
            if (constructorInfo == null)
            {
                throw new NoNullAllowedException();
            }

            var configuration = (DbMigrationsConfiguration) Activator.CreateInstance(configurationType);
            var migrator = new DbMigrator(configuration);
            migrator.Configuration.AutomaticMigrationsEnabled = true;
            migrator.Configuration.AutomaticMigrationDataLossAllowed = _engineConfiguration.MigrateDataLossAllowed;
            migrator.Update();

            _initialized = true;
        }

        private AbstractDomainModel CreateDomainModel()
        {
            return (AbstractDomainModel)_constructor.Invoke(new object[0]);
        }

        public IDomainModel Create()
        {
            if (!_initialized)
            {
                return new DomainModelStub();
            }

            var ret = CreateDomainModel();

            ret.SetSiteId(_siteMapper);

            return ret;
        }

        public IDomainModel CreateWithTransaction()
        {
            var domainModel = Create();
            domainModel.CreateTransaction();
            return domainModel;
        }

        public IDomainModel Create(long? siteId)
        {
            if (!_initialized)
            {
                return new DomainModelStub();
            }

            var ret = CreateDomainModel();
            ret.SetSiteId(siteId);
            return ret;
        }
    }
}
