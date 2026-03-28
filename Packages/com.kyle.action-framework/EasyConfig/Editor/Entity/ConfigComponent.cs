using UnityEngine;
namespace EasyConfig
{
    public abstract class ConfigComponent : ScriptableObject
    {
        public abstract IConfigComponent Export();

    }

    public class TConfigComponent<T> : ConfigComponent where T : IConfigComponent
    {
        public T config;

        public override IConfigComponent Export()
        {
            return config;
        }
    }
}
