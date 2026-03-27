namespace EasyConfig
{
    public class DynimaicListAttribute : NameIndexAttribute
    {
        public string Name { get; private set; }
        public char Separator { get; private set; }
        //如果Separator是默认值，则会使用
        public DynimaicListAttribute(string name, char separator = char.MinValue)
        {
            Name = name;
            Separator = separator;
        }
    }
}
