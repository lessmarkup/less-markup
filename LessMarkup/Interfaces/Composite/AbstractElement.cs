namespace LessMarkup.Interfaces.Composite
{
    public abstract class AbstractElement
    {
        public string Type
        {
            get
            {
                var type = GetType().Name;
                if (type.EndsWith("Element"))
                {
                    type = type.Substring(0, type.Length - "Element".Length);
                }
                return type;
            }
        }
    }
}
