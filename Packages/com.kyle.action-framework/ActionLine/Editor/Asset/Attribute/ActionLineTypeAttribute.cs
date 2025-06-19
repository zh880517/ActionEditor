using System;

namespace ActionLine
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class ActionLineTypeAttribute : Attribute
    {
        public Type AssetType { get; private set; }
        public ActionLineTypeAttribute(Type assetType)
        {
            AssetType = assetType;
        }
    }
}
