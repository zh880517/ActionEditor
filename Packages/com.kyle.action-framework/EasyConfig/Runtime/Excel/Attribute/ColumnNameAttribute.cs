namespace EasyConfig
{
    public class ColumnNameAttribute : NameIndexAttribute
    {
        public string Name { get; private set; }
        public ColumnNameAttribute(string name)
        {
            Name = name;
        }
    }
}