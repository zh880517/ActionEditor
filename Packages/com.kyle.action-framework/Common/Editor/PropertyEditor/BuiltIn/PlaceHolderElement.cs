namespace PropertyEditor.BuiltIn
{
    internal class PlaceHolderElement : PropertyElement
    {
        private bool readOnly = true;
        public override bool ReadOnly 
        { 
            get => readOnly;
            set => readOnly = true;
        }

        public override void SetLable(string name, string tip)
        {
        }

        public override void SetLableWidth(float width)
        {
        }

        public override void SetValue(object value)
        {
        }
    }
}
