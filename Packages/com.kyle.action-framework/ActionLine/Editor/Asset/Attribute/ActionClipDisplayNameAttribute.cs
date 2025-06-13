namespace ActionLine
{
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ActionClipDisplayNameAttribute : System.Attribute
    {
        public string DisplayName { get; private set; }
        public ActionClipDisplayNameAttribute(string displayName)
        {
            DisplayName = displayName;
        }
    }
}
