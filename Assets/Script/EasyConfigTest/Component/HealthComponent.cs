using EasyConfig;

namespace EasyConfigTest
{
    public class HealthComponentData : IConfigComponent
    {
        public float maxHealth = 100f;
        public float currentHealth = 100f;
    }

    [CombatAttribute]
    [Alias("生命组件")]
    public class HealthComponent : TConfigComponent<HealthComponentData>
    {
    }
}
