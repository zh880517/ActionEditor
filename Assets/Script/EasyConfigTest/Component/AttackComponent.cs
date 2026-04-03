using EasyConfig;

namespace EasyConfigTest
{
    public class AttackComponentData : IConfigComponent
    {
        public float damage = 10f;
        public float attackRate = 1f;
    }

    [CombatAttribute]
    [Alias("攻击组件")]
    public class AttackComponent : TConfigComponent<AttackComponentData>
    {
    }
}
