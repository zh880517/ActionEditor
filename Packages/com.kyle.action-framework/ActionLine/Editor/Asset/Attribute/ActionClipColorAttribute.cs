using UnityEngine;

namespace ActionLine
{
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class ActionClipColorAttribute : System.Attribute
    {
        public Color ClipColor { get; private set; }
        public ActionClipColorAttribute(float r, float g, float b)
        {
            ClipColor = new Color(r, g, b, 1);
        }
        public ActionClipColorAttribute(byte r, byte g, byte b)
        {
            ClipColor = new Color32(r, g, b, 255);
        }
    }
}
