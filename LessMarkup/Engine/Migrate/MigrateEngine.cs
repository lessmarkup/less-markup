using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.DataObjects.Common;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Module;

namespace LessMarkup.Engine.Migrate
{
    public class MigrateEngine
    {
        private readonly IModuleProvider _moduleProvider;
        private readonly ILightDomainModelProvider _domainModelProvider;

        public MigrateEngine(IModuleProvider moduleProvider, ILightDomainModelProvider domainModelProvider)
        {
            _moduleProvider = moduleProvider;
            _domainModelProvider = domainModelProvider;
        }

        public void Execute()
        {
            var migrator = Interfaces.DependencyResolver.Resolve<Migrator>();

            if (!migrator.CheckExists<MigrationHistory>())
            {
                migrator.CreateTable<MigrationHistory>();
            }

            using (var domainModel = _domainModelProvider.Create())
            {
                var existingMigrations = new HashSet<string>(domainModel.Query().From<MigrationHistory>().ToList<MigrationHistory>("UniqueId").Select(m => m.UniqueId));

                foreach (var module in _moduleProvider.Modules.Where(m => m.Initializer != null))
                {
                    var assembly = module.Initializer.DataObjectsAssembly;

                    if (assembly == null)
                    {
                        continue;
                    }

                    foreach (var type in assembly.GetTypes().Where(t => typeof (Migration).IsAssignableFrom(t)))
                    {
                        var migration = (Migration) Interfaces.DependencyResolver.TryResolve(type);
                        if (migration == null)
                        {
                            migration = (Migration) Activator.CreateInstance(type);
                            if (migration == null)
                            {
                                continue;
                            }
                        }
                        var uniqueId = migration.Id + "_" + type.Name;
                        if (!existingMigrations.Contains(uniqueId))
                        {
                            migration.Migrate(migrator);
                        }
                        domainModel.Create(new MigrationHistory { Created = DateTime.UtcNow, UniqueId = uniqueId, ModuleType = module.ModuleType});
                    }
                }
            }
        }
    }
}
