using System.Collections.Generic;
using LessMarkup.Framework.NodeHandlers;
using LessMarkup.Interfaces.Composite;

namespace LessMarkup.UserInterface.NodeHandlers.Common
{
    public abstract class CompositeNodeHandler : AbstractNodeHandler
    {
        private readonly List<AbstractElement> _elements = new List<AbstractElement>();

        protected CompositeNodeHandler()
        {
            AddScript("controllers/composite");
        }

        protected void AddElement(AbstractElement element)
        {
            _elements.Add(element);
        }

        protected override Dictionary<string, object> GetViewData()
        {
            return new Dictionary<string, object>()
            {
                { "Elements", _elements }
            };
        }

        protected override string ViewType
        {
            get { return "Composite"; }
        }
    }
}
