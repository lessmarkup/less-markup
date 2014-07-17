using System.Collections.Generic;

namespace LessMarkup.Interfaces.Composite
{
    public class ContainerElement : AbstractElement
    {
        public List<AbstractElement> Elements { get; set; }
    }
}
