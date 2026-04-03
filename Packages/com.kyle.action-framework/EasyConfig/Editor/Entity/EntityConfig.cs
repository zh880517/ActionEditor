using System.Collections.Generic;
using UnityEngine;

namespace EasyConfig
{
    public class EntityConfig : CollectableScriptableObject
    {
        [SerializeField]
        private List<ConfigComponent> components = new List<ConfigComponent>();
        public IReadOnlyList<ConfigComponent> Components => components;

        public virtual void Export()
        {
        }
    }

    public class TEntityConfig<T> : EntityConfig where T : IEntityConfig
    {
        
    }
}

