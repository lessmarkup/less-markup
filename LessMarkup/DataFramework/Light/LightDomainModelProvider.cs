using System.Linq;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Module;
using LessMarkup.Interfaces.System;

namespace LessMarkup.DataFramework.Light
{
    public class LightDomainModelProvider : ILightDomainModelProvider, IInitialize
    {
        public ILightDomainModel Create()
        {
            var domainModel = Interfaces.DependencyResolver.Resolve<ILightDomainModel>();
            return domainModel;
        }

        public ILightDomainModel CreateWithTransaction()
        {
            var domainModel = (LightDomainModel) Interfaces.DependencyResolver.Resolve<ILightDomainModel>();
            domainModel.CreateTransaction();
            return domainModel;
        }

        public void Initialize(params object[] arguments)
        {
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
                    LightDomainModel.RegisterDataType(dataType);
                }
            }
        }
    }
}
