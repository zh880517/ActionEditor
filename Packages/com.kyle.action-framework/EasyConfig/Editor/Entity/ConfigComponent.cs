using UnityEngine;
namespace EasyConfig
{
    public abstract class ConfigComponent : ScriptableObject
    {
        [HiddenInPropertyEditor]
        public bool Enable;
        public abstract IConfigComponent Export();

    }

    public class TConfigComponent<T> : ConfigComponent where T : IConfigComponent, new ()
    {
        [ExpandedInParent]
        public T config = new T();

        public override IConfigComponent Export()
        {
            return config;
        }
    }
}
