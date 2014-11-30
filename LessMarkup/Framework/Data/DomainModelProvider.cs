using System.Linq;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Module;
using LessMarkup.Interfaces.System;

namespace LessMarkup.Framework.Data
{
    public class DomainModelProvider : IDomainModelProvider, IInitialize
    {
        public IDomainModel Create()
        {
            var domainModel = Interfaces.DependencyResolver.Resolve<IDomainModel>();
            return domainModel;
        }

        public IDomainModel CreateWithTransaction()
        {
            var domainModel = (DomainModel) Interfaces.DependencyResolver.Resolve<IDomainModel>();
            domainModel.CreateTransaction();
            return domainModel;
        }

        public void Initialize(params object[] arguments)
        {
            this.LogDebug("Initializing Domain Model Provider");

            var moduleProvider = Interfaces.DependencyResolver.Resolve<IModuleProvider>();

            foreach (var module in moduleProvider.Modules.Where(m => m.Initializer != null))
            {
                var assembly = module.Initializer.DataObjectsAssembly;

                if (assembly == null)
                {
                    continue;
                }

                foreach (var dataType in assembly.GetTypes().Where(t => typeof (IDataObject).IsAssignableFrom(t)))
                {
                    this.LogDebug(string.Format("Registering data type '{0}'", dataType.FullName));
                    DomainModel.RegisterDataType(dataType);
                }
            }
        }
    }
}
