using System;

namespace ActionLine.EditorView
{
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
    public class CustomClipSimulatorAttribute : Attribute
    {
        public Type ClipType { get; private set; }
        public CustomClipSimulatorAttribute(Type clipType)
        {
            ClipType = clipType;
        }
    }
}
